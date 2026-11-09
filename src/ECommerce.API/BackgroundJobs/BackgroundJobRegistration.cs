namespace ECommerce.API.BackgroundJobs;

public static class BackgroundJobRegistration
{
    public static IServiceCollection AddBackgroundJobs(this IServiceCollection services)
    {
        services.AddHostedService<TrendyolBatchStatusWorker>();
        services.AddHostedService<TrendyolInventorySyncWorker>();
        services.AddHostedService<TrendyolOrderImportWorker>();
        services.AddHostedService<TrendyolOrderStatusWorker>();
        return services;
    }
}
