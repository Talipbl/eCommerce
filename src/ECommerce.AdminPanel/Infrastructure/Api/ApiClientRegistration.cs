using ECommerce.AdminPanel.Api;
using Microsoft.Extensions.Options;

namespace ECommerce.AdminPanel.Infrastructure;

public static class ApiClientRegistration
{
    /// <summary>Panelin kullandığı yönetici client'larını typed HttpClient olarak kaydeder. Müşteri ve vitrin client'ları kaydedilmez.</summary>
    public static IServiceCollection AddApiClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ApiOptions>()
            .Bind(configuration.GetSection(ApiOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddApiClient<IAuthClient, AuthClient>();
        services.AddApiClient<ICategoriesClient, CategoriesClient>();
        services.AddApiClient<IBrandsClient, BrandsClient>();
        services.AddApiClient<IProductsClient, ProductsClient>();
        services.AddApiClient<IOrdersClient, OrdersClient>();
        services.AddApiClient<IMarketplaceSyncClient, MarketplaceSyncClient>();
        services.AddApiClient<ITrendyolCatalogClient, TrendyolCatalogClient>();
        services.AddApiClient<ITrendyolIntegrationClient, TrendyolIntegrationClient>();
        services.AddApiClient<ITrendyolBatchesClient, TrendyolBatchesClient>();

        return services;
    }

    private static void AddApiClient<TClient, TImplementation>(this IServiceCollection services)
        where TClient : class
        where TImplementation : class, TClient =>
        services.AddHttpClient<TClient, TImplementation>((provider, http) =>
        {
            var options = provider.GetRequiredService<IOptions<ApiOptions>>().Value;
            // Üretilen client göreli yol kullanır ("api/v1/..."); taban adres '/' ile bitmeli.
            http.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            http.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
}
