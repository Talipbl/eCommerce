using ECommerce.Application.Shopping;
using ECommerce.Catalog;
using ECommerce.Core.Persistence;
using ECommerce.Marketplaces;

namespace ECommerce.Application.Marketplaces;

public interface IMarketplaceInventorySyncService
{
    Task<int> SyncPendingAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Stok/fiyat outbox'ını her pazaryerinde kabul edilmiş ürünler için toplu gönderir. Rate limit'te pazaryeri beklemeye alınır,
/// geçici hatalar artan aralıkla tekrar denenir, reddedilen istekler hata mesajıyla kapatılır.
/// </summary>
public class MarketplaceInventorySyncService(
    IEnumerable<IMarketplaceProvider> providers,
    IRepository<MarketplaceInventoryChange> changes,
    IRepository<MarketplaceListing> listings,
    IRepository<Product> products,
    IRepository<ProductVariant> variants,
    IRepository<MarketplaceSyncState> states,
    IRepository<MarketplaceSyncLog> logs,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : IMarketplaceInventorySyncService
{
    private const int BatchSize = 1000;

    public async Task<int> SyncPendingAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var pending = await changes.ListAsync(q => q
            .Where(c => c.Status == OutboxStatus.Pending && (c.NextAttemptAt == null || c.NextAttemptAt <= now))
            .OrderBy(c => c.Id).Take(BatchSize), cancellationToken);
        if (pending.Count == 0)
            return 0;

        var found = await CartService.LoadVariantsAsync(variants, products, pending.Select(c => c.VariantId).Distinct().ToList(), cancellationToken);
        var productIds = found.Values.Select(v => v.Product.Id).Distinct().ToList();
        string? transient = null, rejected = null;
        var deferred = false;

        foreach (var provider in providers)
        {
            var syncable = (await listings.ListAsync(l => l.Marketplace == provider.Name && productIds.Contains(l.ProductId), cancellationToken))
                .Where(l => l.IsSyncable).Select(l => l.ProductId).ToHashSet();

            // Satıştan kalkan ürün veya varyant pazaryerinde 0 stokla satışa kapatılır.
            var items = found.Values
                .Where(v => syncable.Contains(v.Product.Id))
                .Select(v => new MarketplacePriceStock(v.Variant.Barcode, v.IsPurchasable ? v.Variant.StockQuantity : 0, v.Variant.Price, v.Variant.ListPrice ?? v.Variant.Price))
                .ToList();
            if (items.Count == 0)
                continue;

            var state = await states.GetOrCreateAsync(provider.Name, MarketplaceSyncJobs.PriceInventory, cancellationToken);
            if (state.IsPaused(now))
            {
                deferred = true;
                continue;
            }

            foreach (var chunk in items.Chunk(provider.MaxPriceInventoryItems))
            {
                var result = await provider.PushPriceInventoryAsync(chunk, cancellationToken);
                var message = result.Succeeded ? $"{chunk.Length} ürün gönderildi." : result.Error!.Describe();
                logs.Add(MarketplaceSyncLog.Write(provider.Name, SyncOperation.PriceInventory, null, result.Succeeded, message, now));
                state.RecordRun(now, result.Succeeded ? null : message);
                if (result.Succeeded)
                    continue;

                if (result.Error!.Kind == MarketplaceErrorKind.RateLimited)
                {
                    state.PauseUntil(now + MarketplaceProviders.RateLimitPause);
                    deferred = true;
                }
                else if (result.Error.IsTransient())
                    transient = message;
                else
                    rejected = message;
                break;
            }
        }

        var processed = 0;
        foreach (var change in pending)
        {
            if (rejected is not null)
                change.MarkFailed(rejected, now);
            else if (transient is not null)
                change.MarkTransientFailure(transient, now);
            else if (!deferred)
            {
                change.MarkSucceeded(now);
                processed++;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return processed;
    }
}

internal static class SyncStateRepository
{
    public static async Task<MarketplaceSyncState> GetOrCreateAsync(this IRepository<MarketplaceSyncState> states, string marketplace, string job, CancellationToken cancellationToken)
    {
        var state = await states.FirstOrDefaultAsync(s => s.Marketplace == marketplace && s.Job == job, cancellationToken);
        if (state is null)
            states.Add(state = MarketplaceSyncState.Create(marketplace, job));
        return state;
    }
}
