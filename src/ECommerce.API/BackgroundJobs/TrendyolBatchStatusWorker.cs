using ECommerce.Application.Marketplaces.Trendyol;

namespace ECommerce.API.BackgroundJobs;

public class TrendyolBatchStatusWorker(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<TrendyolBatchStatusWorker> logger)
    : PeriodicTrendyolWorker(scopeFactory, configuration, logger)
{
    protected override string Section => "BatchPolling";

    protected override int DefaultIntervalSeconds => 60;

    protected override async Task<string?> RunAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var batches = await services.GetRequiredService<ITrendyolBatchService>().RefreshPendingAsync(cancellationToken);
        var listings = await services.GetRequiredService<ITrendyolListingService>().RefreshStatusesAsync(cancellationToken);
        return batches + listings > 0 ? $"{batches} batch, {listings} listing güncellendi" : null;
    }
}
