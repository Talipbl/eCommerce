using ECommerce.Admin;
using ECommerce.Application.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;

namespace ECommerce.IntegrationTests.Infrastructure;

public class ApiFactory(
    string connectionString,
    IReadOnlyDictionary<string, string?>? settings = null,
    Action<IServiceCollection>? configureServices = null) : WebApplicationFactory<Program>
{
    public const string SigningKey = "integration-test-signing-key-0123456789abcdef";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:ECommerce", connectionString);
        builder.UseSetting("Trendyol:SellerId", "1");
        builder.UseSetting("Trendyol:ApiKey", "test-key");
        builder.UseSetting("Trendyol:ApiSecret", "test-secret");
        builder.UseSetting("Trendyol:BatchPolling:Enabled", "false");
        builder.UseSetting("Trendyol:InventorySync:Enabled", "false");
        builder.UseSetting("Trendyol:OrderImport:Enabled", "false");
        builder.UseSetting("Trendyol:OrderStatusSync:Enabled", "false");
        builder.UseSetting("Jwt:SigningKey", SigningKey);
        builder.UseSetting("RateLimiting:Auth:PermitLimit", "1000");

        foreach (var (key, value) in settings ?? new Dictionary<string, string?>())
            builder.UseSetting(key, value);

        builder.ConfigureTestServices(services =>
        {
            services.AddControllers().AddApplicationPart(typeof(ApiFactory).Assembly);
            configureServices?.Invoke(services);
        });
    }

    /// <summary>Veritabanına gitmeden geçerli bir yönetici token'ıyla istemci oluşturur.</summary>
    public HttpClient CreateAdminClient()
    {
        var token = Services.GetRequiredService<ITokenService>()
            .CreateAccessToken(new DefAdmin { Id = 1, UserName = "test-admin", Role = AdminRoles.Admin }, DateTime.UtcNow).Token;

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
