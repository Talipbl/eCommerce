namespace ECommerce.Application.Marketplaces;

public interface IMarketplaceOrderStatusSyncService
{
    Task<int> SyncPendingAsync(CancellationToken cancellationToken = default);
}
