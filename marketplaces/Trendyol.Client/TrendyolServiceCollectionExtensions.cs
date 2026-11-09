using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using Trendyol.Client.Catalog;
using Trendyol.Client.Orders;
using Trendyol.Client.Products;

namespace Trendyol.Client
{
    public static class TrendyolServiceCollectionExtensions
    {
        public static IServiceCollection AddTrendyol(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<TrendyolOptions>()
                .Bind(configuration.GetSection(TrendyolOptions.SectionName))
                .Validate(o => o.IsConfigured, "Trendyol ayarları eksik. SellerId, ApiKey ve ApiSecret değerlerini user-secrets veya ortam değişkenleri ile tanımlayın.")
                .Validate(o => Uri.TryCreate(o.BaseUrl, UriKind.Absolute, out _), "Trendyol:BaseUrl geçerli bir adres değil.")
                .ValidateOnStart();

            services.AddHttpClient<ITrendyolCatalogClient, TrendyolCatalogClient>(ConfigureClient).AddResilience();
            services.AddHttpClient<ITrendyolProductClient, TrendyolProductClient>(ConfigureClient).AddResilience();
            services.AddHttpClient<ITrendyolOrderClient, TrendyolOrderClient>(ConfigureClient).AddResilience();

            return services;
        }

        // Ürün yaratma ve fiyat güncelleme gibi işlemler idempotent olmadığı için POST/PUT/DELETE tekrar denenmez.
        private static void AddResilience(this IHttpClientBuilder builder) =>
            builder.AddStandardResilienceHandler(options => options.Retry.DisableForUnsafeHttpMethods());

        private static void ConfigureClient(IServiceProvider provider, HttpClient client)
        {
            var options = provider.GetRequiredService<IOptions<TrendyolOptions>>().Value;

            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", options.BasicAuthParameter);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
    }
}
