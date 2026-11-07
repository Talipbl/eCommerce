using ECommerce.Catalog;
using ECommerce.Core.Persistence;
using ECommerce.Marketplaces;
using ECommerce.Ordering;

namespace ECommerce.Application.Marketplaces;

public record OrderImportSummary(string Marketplace, int Imported, int Updated, int Skipped, string? Error = null);

public interface IMarketplaceOrderImportService
{
    Task<IReadOnlyList<OrderImportSummary>> ImportAllAsync(CancellationToken cancellationToken = default);

    Task<OrderImportSummary> ImportAsync(string marketplace, CancellationToken cancellationToken = default);
}

/// <summary>
/// Pazaryeri siparişlerini son çekim zamanından (imleç) itibaren, pazaryerinin izin verdiği pencerelerle ve son güncellenme
/// tarihine göre içe alır. Dış referans tekil olduğundan aynı paket iki kez sipariş olmaz; sonraki çekimlerde durum yansıtılır.
/// </summary>
public class MarketplaceOrderImportService(
    IEnumerable<IMarketplaceProvider> providers,
    IRepository<Order> orders,
    IRepository<Product> products,
    IRepository<ProductVariant> variants,
    IRepository<MarketplaceSyncState> states,
    IRepository<MarketplaceSyncLog> logs,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : IMarketplaceOrderImportService
{
    /// <summary>Saat farkı ve sınırdaki güncellemeler için pencereler bu kadar geriden başlar.</summary>
    public static readonly TimeSpan Overlap = TimeSpan.FromMinutes(5);

    public async Task<IReadOnlyList<OrderImportSummary>> ImportAllAsync(CancellationToken cancellationToken = default)
    {
        var summaries = new List<OrderImportSummary>();
        foreach (var provider in providers)
            summaries.Add(await ImportAsync(provider, cancellationToken));
        return summaries;
    }

    public Task<OrderImportSummary> ImportAsync(string marketplace, CancellationToken cancellationToken = default)
    {
        var provider = providers.FirstOrDefault(p => p.Name == marketplace)
            ?? throw new InvalidOperationException($"'{marketplace}' için pazaryeri sağlayıcısı kayıtlı değil.");
        return ImportAsync(provider, cancellationToken);
    }

    private async Task<OrderImportSummary> ImportAsync(IMarketplaceProvider provider, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var state = await states.GetOrCreateAsync(provider.Name, MarketplaceSyncJobs.OrderImport, cancellationToken);
        if (state.IsPaused(now.UtcDateTime))
            return new OrderImportSummary(provider.Name, 0, 0, 0, $"Rate limit nedeniyle {state.PausedUntil:u} sonrasına kadar bekleniyor.");

        var earliest = now - provider.MaxOrderLookback + Overlap;
        var from = state.Cursor is { } cursor ? new DateTimeOffset(DateTime.SpecifyKind(cursor, DateTimeKind.Utc)) - Overlap : now - provider.MaxOrderWindow;
        if (from < earliest)
            from = earliest;

        int imported = 0, updated = 0, skipped = 0;
        while (from < now)
        {
            var to = from + provider.MaxOrderWindow < now ? from + provider.MaxOrderWindow : now;
            for (var page = 0; page < provider.MaxOrderPages; page++)
            {
                var response = await provider.FetchOrdersAsync(from, to, page, cancellationToken);
                if (!response.Succeeded)
                {
                    var error = response.Error!.Describe();
                    if (response.Error!.Kind == MarketplaceErrorKind.RateLimited)
                        state.PauseUntil(now.UtcDateTime + MarketplaceProviders.RateLimitPause);
                    return await FinishAsync(provider, state, now, new OrderImportSummary(provider.Name, imported, updated, skipped, error), cancellationToken);
                }

                foreach (var order in response.Value!.Orders)
                {
                    switch (await ProcessAsync(provider, order, now.UtcDateTime, cancellationToken))
                    {
                        case Outcome.Imported: imported++; break;
                        case Outcome.Updated: updated++; break;
                        default: skipped++; break;
                    }
                }

                await unitOfWork.SaveChangesAsync(cancellationToken);
                if (page + 1 >= response.Value.TotalPages)
                    break;
            }

            state.AdvanceCursor(to.UtcDateTime);
            from = to;
        }

        return await FinishAsync(provider, state, now, new OrderImportSummary(provider.Name, imported, updated, skipped), cancellationToken);
    }

    private async Task<OrderImportSummary> FinishAsync(
        IMarketplaceProvider provider, MarketplaceSyncState state, DateTimeOffset now, OrderImportSummary summary, CancellationToken cancellationToken)
    {
        state.RecordRun(now.UtcDateTime, summary.Error);
        if (summary.Error is not null || summary.Imported + summary.Updated > 0)
        {
            var message = summary.Error ?? $"{summary.Imported} yeni, {summary.Updated} güncellenen, {summary.Skipped} atlanan sipariş.";
            logs.Add(MarketplaceSyncLog.Write(provider.Name, SyncOperation.OrderImport, null, summary.Error is null, message, now.UtcDateTime));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return summary;
    }

    private enum Outcome { Imported, Updated, Skipped }

    private async Task<Outcome> ProcessAsync(IMarketplaceProvider provider, MarketplaceOrder source, DateTime now, CancellationToken cancellationToken)
    {
        var existing = await orders.FirstOrDefaultAsync(o => o.Channel == provider.Channel && o.ExternalReference == source.ExternalReference, cancellationToken);
        if (existing is not null)
        {
            if (source.Status is null || !existing.ApplyMarketplaceStatus(source.Status.Value, source.CargoProvider, source.TrackingNumber, now))
                return Outcome.Skipped;
            if (source.Status == OrderStatus.Cancelled)
                await AdjustStockAsync(existing.Lines.Where(l => l.VariantId is not null).Select(l => (l.VariantId!.Value, l.Quantity)), increase: true, cancellationToken);
            return Outcome.Updated;
        }

        if (source.Status is null or OrderStatus.Cancelled)
            return Outcome.Skipped;

        var barcodes = source.Lines.Select(l => l.Barcode).OfType<string>().Distinct().ToList();
        var byBarcode = (await variants.ListAsync(v => barcodes.Contains(v.Barcode), cancellationToken)).ToDictionary(v => v.Barcode);
        var inputs = source.Lines.Select(l =>
        {
            var variant = l.Barcode is null ? null : byBarcode.GetValueOrDefault(l.Barcode);
            return new OrderLineInput(variant?.Id, variant?.ProductId, l.ProductName, null, l.Barcode ?? "-", l.Sku,
                l.UnitPrice, Math.Max(l.Quantity, 1), l.VatRate, l.ExternalLineId);
        }).ToList();

        var order = Order.ImportMarketplaceOrder(
            provider.OrderNumberPrefix + source.ExternalReference, provider.Channel, source.ExternalReference, source.CustomerEmail, source.CustomerName,
            source.Address, inputs, source.PlacedAt, source.CargoProvider, source.TrackingNumber);
        if (source.Status != OrderStatus.Confirmed)
            order.ApplyMarketplaceStatus(source.Status.Value, source.CargoProvider, source.TrackingNumber, now);

        orders.Add(order);
        await AdjustStockAsync(inputs.Where(i => i.VariantId is not null).Select(i => (i.VariantId!.Value, i.Quantity)), increase: false, cancellationToken);
        return Outcome.Imported;
    }

    // Satış pazaryerinde gerçekleştiği için yerel stok yetmese de sipariş alınır; stok sıfırın altına inmez.
    private async Task AdjustStockAsync(IEnumerable<(int VariantId, int Quantity)> lines, bool increase, CancellationToken cancellationToken)
    {
        var quantities = lines.GroupBy(l => l.VariantId).ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));
        if (quantities.Count == 0)
            return;

        var productIds = await variants.ListAsync(q => q.Where(v => quantities.Keys.Contains(v.Id)).Select(v => v.ProductId), cancellationToken);
        foreach (var product in await products.ListAsync(p => productIds.Contains(p.Id), cancellationToken))
        {
            foreach (var variant in product.Variants.Where(v => quantities.ContainsKey(v.Id)))
            {
                var quantity = quantities[variant.Id];
                if (increase)
                    variant.IncreaseStock(quantity);
                else
                    variant.SetStock(Math.Max(0, variant.StockQuantity - quantity));
            }
        }
    }
}
