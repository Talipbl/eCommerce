using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ECommerce.AdminPanel.Tests;

public sealed class PanelFactory(Dictionary<string, string?>? settings = null) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("Api:BaseUrl", "http://api.test/");
        builder.UseSetting("Media:PublicBaseUrl", "https://shop.test");
        foreach (var (key, value) in settings ?? [])
            builder.UseSetting(key, value);
    }
}
