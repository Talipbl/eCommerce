using ECommerce.Application.Marketplaces;

namespace ECommerce.API.BackgroundJobs;

public class TrendyolOrderStatusWorker(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<TrendyolOrderStatusWorker> logger)
    : PeriodicTrendyolWorker(scopeFactory, configuration, logger)
{
    protected override string Section => "OrderStatusSync";

    protected override int DefaultIntervalSeconds => 60;

    protected override async Task<string?> RunAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var processed = await services.GetRequiredService<IMarketplaceOrderStatusSyncService>().SyncPendingAsync(cancellationToken);
        return processed > 0 ? $"{processed} sipariş durumu bildirildi" : null;
    }
}
