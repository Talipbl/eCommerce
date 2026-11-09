using ECommerce.Application.Marketplaces;
using ECommerce.Catalog;
using ECommerce.Core.Persistence;
using ECommerce.Marketplaces;
using ECommerce.Ordering;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Options;

namespace ECommerce.Application.Dashboard;

/// <summary>Yönetim paneli ana sayfası: sipariş durumları, ciro, düşük stok ve senkron uyarıları.</summary>
public class DashboardService(
    IRepository<Order> orders,
    IRepository<Product> products,
    IRepository<MarketplaceInventoryChange> inventory,
    IRepository<MarketplaceOrderStatusUpdate> statusUpdates,
    IRepository<MarketplaceSyncState> states,
    IOptions<DashboardOptions> options,
    TimeProvider clock,
    IMapper mapper) : IDashboardService
{
    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var now = clock.GetUtcNow().UtcDateTime;
        var today = StartOfLocalDay(now, TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZone));

        return new DashboardSummaryDto(
            await CountByStatusAsync(cancellationToken),
            await orders.CountAsync(o => o.Status == OrderStatus.PendingPayment && o.PaymentMethod == PaymentMethod.BankTransfer, cancellationToken),
            await RevenueSinceAsync(today, cancellationToken),
            await RevenueSinceAsync(today.AddDays(-6), cancellationToken),
            await LowStockAsync(settings, cancellationToken),
            new FailedOutboxDto(
                await inventory.CountAsync(c => c.Status == OutboxStatus.Failed, cancellationToken),
                await statusUpdates.CountAsync(u => u.Status == OutboxStatus.Failed, cancellationToken)),
            await states.ListAsync(q => q.Where(s => s.LastError != null || s.PausedUntil > now)
                .OrderBy(s => s.Marketplace).ThenBy(s => s.Job)
                .ProjectToType<SyncStateDto>(mapper.Config), cancellationToken));
    }

    // Gün sınırı yerel saate göre çizilir, sorgu UTC ile yapılır (siparişler UTC saklanıyor).
    private static DateTime StartOfLocalDay(DateTime utcNow, TimeZoneInfo zone) =>
        TimeZoneInfo.ConvertTimeToUtc(TimeZoneInfo.ConvertTimeFromUtc(utcNow, zone).Date, zone);

    private async Task<IReadOnlyList<OrderStatusCountDto>> CountByStatusAsync(CancellationToken cancellationToken)
    {
        var counts = (await orders.ListAsync(q => q.GroupBy(o => o.Status).Select(g => new { Status = g.Key, Count = g.Count() }), cancellationToken))
            .ToDictionary(c => c.Status, c => c.Count);
        return Enum.GetValues<OrderStatus>().Select(s => new OrderStatusCountDto(s, counts.GetValueOrDefault(s))).ToList();
    }

    private async Task<RevenueDto> RevenueSinceAsync(DateTime from, CancellationToken cancellationToken)
    {
        var channels = await orders.ListAsync(q => q
            .Where(o => o.Status != OrderStatus.Cancelled && o.PlacedAt >= from)
            .GroupBy(o => o.Channel)
            .Select(g => new ChannelRevenueDto(g.Key, g.Sum(o => o.Total), g.Count())), cancellationToken);
        var ordered = channels.OrderBy(c => c.Channel).ToList();
        return new RevenueDto(from, ordered.Sum(c => c.Total), ordered.Sum(c => c.OrderCount), ordered);
    }

    private async Task<LowStockDto> LowStockAsync(DashboardOptions settings, CancellationToken cancellationToken)
    {
        IQueryable<LowStockVariantDto> LowStock(IQueryable<Product> q) => q
            .Where(p => p.IsActive)
            .SelectMany(p => p.Variants.Where(v => v.IsActive && v.StockQuantity <= settings.LowStockThreshold),
                (p, v) => new LowStockVariantDto(p.Id, p.Name, v.Id, v.Sku, v.Barcode, v.StockQuantity));

        var total = await products.CountAsync(LowStock, cancellationToken);
        var variants = await products.ListAsync(q => LowStock(q)
            .OrderBy(v => v.StockQuantity).ThenBy(v => v.ProductName).ThenBy(v => v.Sku)
            .Take(settings.LowStockListSize), cancellationToken);
        return new LowStockDto(settings.LowStockThreshold, total, variants);
    }
}
