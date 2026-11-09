using ECommerce.AdminPanel.Api;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ECommerce.AdminPanel.Infrastructure;

public static class ApiClientRegistration
{
    /// <summary>
    /// Panelin kullandığı yönetici client'larını typed HttpClient olarak kaydeder; her istek oturumun token'ını ve istemci IP'sini taşır.
    /// Müşteri ve vitrin client'ları kaydedilmez.
    /// </summary>
    public static IServiceCollection AddApiClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ApiOptions>()
            .Bind(configuration.GetSection(ApiOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpContextAccessor();
        services.TryAddSingleton(TimeProvider.System);
        services.AddTransient<ForwardedForHandler>();
        services.AddTransient<ApiTokenHandler>();

        services.AddHttpClient<IApiAuthClient, ApiAuthClient>().ConfigureApiHttpClient();

        services.AddApiClient<IAuthClient, AuthClient>();
        services.AddApiClient<IDashboardClient, DashboardClient>();
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
        services.AddHttpClient<TClient, TImplementation>()
            .ConfigureApiHttpClient()
            .AddHttpMessageHandler<ApiTokenHandler>();

    private static IHttpClientBuilder ConfigureApiHttpClient(this IHttpClientBuilder builder) => builder
        .ConfigureHttpClient((provider, http) =>
        {
            var options = provider.GetRequiredService<IOptions<ApiOptions>>().Value;
            // Üretilen client göreli yol kullanır ("api/v1/..."); taban adres '/' ile bitmeli.
            http.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            http.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        })
        // Havuzdaki handler'lar oturumlar arasında paylaşıldığı için cookie container kapalı; refresh cookie'si elle taşınır.
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { UseCookies = false })
        .AddHttpMessageHandler<ForwardedForHandler>();
}
