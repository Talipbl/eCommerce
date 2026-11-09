using ECommerce.Application.Marketplaces;

namespace ECommerce.API.BackgroundJobs;

public class TrendyolOrderImportWorker(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<TrendyolOrderImportWorker> logger)
    : PeriodicTrendyolWorker(scopeFactory, configuration, logger)
{
    protected override string Section => "OrderImport";

    protected override int DefaultIntervalSeconds => 300;

    protected override async Task<string?> RunAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var summaries = await services.GetRequiredService<IMarketplaceOrderImportService>().ImportAllAsync(cancellationToken);
        var changed = summaries.Where(s => s.Imported + s.Updated > 0 || s.Error is not null)
            .Select(s => $"{s.Marketplace}: {s.Imported} yeni, {s.Updated} güncellenen sipariş{(s.Error is null ? string.Empty : $"; hata: {s.Error}")}")
            .ToList();
        return changed.Count > 0 ? string.Join(" | ", changed) : null;
    }
}
