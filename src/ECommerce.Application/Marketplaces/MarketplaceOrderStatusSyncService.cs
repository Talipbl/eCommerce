using ECommerce.Core.Persistence;
using ECommerce.Marketplaces;
using ECommerce.Ordering;

namespace ECommerce.Application.Marketplaces;

public interface IMarketplaceOrderStatusSyncService
{
    Task<int> SyncPendingAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Yerelde ilerletilen pazaryeri siparişlerinin durumunu (Picking, Invoiced) sırayla bildirir. Aynı siparişin önceki bildirimi
/// beklerken sonraki gönderilmez; kalıcı hata sıradakileri engellemez.
/// </summary>
public class MarketplaceOrderStatusSyncService(
    IEnumerable<IMarketplaceProvider> providers,
    IRepository<MarketplaceOrderStatusUpdate> updates,
    IRepository<Order> orders,
    IRepository<MarketplaceSyncState> states,
    IRepository<MarketplaceSyncLog> logs,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : IMarketplaceOrderStatusSyncService
{
    private const int BatchSize = 50;

    public async Task<int> SyncPendingAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var dueOrderIds = await updates.ListAsync(q => q
            .Where(u => u.Status == OutboxStatus.Pending && (u.NextAttemptAt == null || u.NextAttemptAt <= now))
            .OrderBy(u => u.Id).Select(u => u.OrderId).Distinct().Take(BatchSize), cancellationToken);
        if (dueOrderIds.Count == 0)
            return 0;

        var pending = await updates.ListAsync(q => q.Where(u => u.Status == OutboxStatus.Pending && dueOrderIds.Contains(u.OrderId)).OrderBy(u => u.Id), cancellationToken);
        var orderById = (await orders.ListAsync(o => dueOrderIds.Contains(o.Id), cancellationToken)).ToDictionary(o => o.Id);
        var blockedOrders = new HashSet<int>();
        var pausedMarketplaces = new HashSet<string>();
        var processed = 0;

        foreach (var update in pending)
        {
            if (blockedOrders.Contains(update.OrderId) || pausedMarketplaces.Contains(update.Marketplace) || !update.IsDue(now))
            {
                blockedOrders.Add(update.OrderId);
                continue;
            }

            var provider = providers.FirstOrDefault(p => p.Name == update.Marketplace);
            if (provider is null || !orderById.TryGetValue(update.OrderId, out var order))
            {
                Fail(update, provider is null ? "Pazaryeri sağlayıcısı kayıtlı değil." : "Sipariş bulunamadı.", now);
                processed++;
                continue;
            }

            var state = await states.GetOrCreateAsync(provider.Name, MarketplaceSyncJobs.OrderStatus, cancellationToken);
            if (state.IsPaused(now))
            {
                pausedMarketplaces.Add(update.Marketplace);
                blockedOrders.Add(update.OrderId);
                continue;
            }

            if (order.Lines.Any(l => l.ExternalLineId is null))
            {
                Fail(update, "Siparişte pazaryeri satır numarası olmayan kalem var.", now);
                processed++;
                continue;
            }

            var command = new MarketplaceStatusCommand(update.ExternalReference, update.Action,
                order.Lines.Select(l => new MarketplaceLineQuantity(l.ExternalLineId!.Value, l.Quantity)).ToList(), order.InvoiceNumber);
            var result = await provider.UpdateOrderStatusAsync(command, cancellationToken);
            state.RecordRun(now, result.Succeeded ? null : result.Error!.Describe());

            if (result.Succeeded)
            {
                update.MarkSucceeded(now);
                logs.Add(MarketplaceSyncLog.Write(update.Marketplace, SyncOperation.OrderStatus, update.ExternalReference, true, $"{update.Action} bildirildi.", now));
                processed++;
            }
            else if (result.Error!.Kind == MarketplaceErrorKind.RateLimited)
            {
                state.PauseUntil(now + MarketplaceProviders.RateLimitPause);
                pausedMarketplaces.Add(update.Marketplace);
                blockedOrders.Add(update.OrderId);
            }
            else if (result.Error.IsTransient())
            {
                update.MarkTransientFailure(result.Error.Describe(), now);
                logs.Add(MarketplaceSyncLog.Write(update.Marketplace, SyncOperation.OrderStatus, update.ExternalReference, false,
                    $"{update.Action}: {result.Error.Describe()} (deneme {update.Attempts})", now));
                blockedOrders.Add(update.OrderId);
            }
            else
            {
                Fail(update, result.Error.Describe(), now);
                processed++;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return processed;
    }

    private void Fail(MarketplaceOrderStatusUpdate update, string error, DateTime now)
    {
        update.MarkFailed(error, now);
        logs.Add(MarketplaceSyncLog.Write(update.Marketplace, SyncOperation.OrderStatus, update.ExternalReference, false, $"{update.Action}: {error}", now));
    }
}
