namespace ECommerce.API.BackgroundJobs;

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
