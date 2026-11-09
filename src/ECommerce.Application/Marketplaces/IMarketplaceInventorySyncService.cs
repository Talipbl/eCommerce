namespace ECommerce.Application.Marketplaces;

public interface IMarketplaceInventorySyncService
{
    Task<int> SyncPendingAsync(CancellationToken cancellationToken = default);
}
