using ECommerce.Application.Marketplaces;
using ECommerce.Application.Marketplaces.Trendyol;

namespace ECommerce.API.Infrastructure;

/// <summary>
/// Ayarlardaki aralıkla kendi scope'unda çalışan arka plan işi ("Trendyol:{Section}:Enabled" ve ":IntervalSeconds").
/// </summary>
public abstract class PeriodicTrendyolWorker(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger logger) : BackgroundService
{
    protected abstract string Section { get; }

    protected abstract int DefaultIntervalSeconds { get; }

    protected abstract Task<string?> RunAsync(IServiceProvider services, CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var section = configuration.GetSection($"Trendyol:{Section}");
        if (!section.GetValue("Enabled", true))
            return;

        var interval = TimeSpan.FromSeconds(Math.Max(10, section.GetValue("IntervalSeconds", DefaultIntervalSeconds)));
        using var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var summary = await RunAsync(scope.ServiceProvider, stoppingToken);
                if (summary is not null)
                    logger.LogInformation("Trendyol {Job}: {Summary}", Section, summary);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Trendyol {Job} işi başarısız oldu.", Section);
            }
        }
    }
}

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
