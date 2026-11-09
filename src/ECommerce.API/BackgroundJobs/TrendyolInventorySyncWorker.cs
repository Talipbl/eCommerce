using ECommerce.Application.Marketplaces;

namespace ECommerce.API.BackgroundJobs;

public class TrendyolInventorySyncWorker(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<TrendyolInventorySyncWorker> logger)
    : PeriodicTrendyolWorker(scopeFactory, configuration, logger)
{
    protected override string Section => "InventorySync";

    protected override int DefaultIntervalSeconds => 60;

    protected override async Task<string?> RunAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var processed = await services.GetRequiredService<IMarketplaceInventorySyncService>().SyncPendingAsync(cancellationToken);
        return processed > 0 ? $"{processed} stok/fiyat değişikliği işlendi" : null;
    }
}
