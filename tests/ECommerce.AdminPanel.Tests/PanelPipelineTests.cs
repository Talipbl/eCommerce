using AngleSharp.Html.Parser;
using ECommerce.AdminPanel.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;

namespace ECommerce.AdminPanel.Tests;

public class PanelPipelineTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Responses_include_strict_security_headers()
    {
        await using var factory = new PanelFactory();

        var response = await factory.CreatePanelClient().GetAsync("/account/login", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.GetValues("X-Frame-Options").ShouldContain("DENY");
        response.Headers.GetValues("X-Content-Type-Options").ShouldContain("nosniff");
        var csp = response.Headers.GetValues("Content-Security-Policy").Single();
        csp.ShouldContain("script-src 'self';");
        csp.ShouldContain("img-src 'self' data: https://shop.test;");
        csp.ShouldContain("frame-ancestors 'none'");
        csp.ShouldNotContain("unsafe-inline");
    }

    [Theory]
    [InlineData("/account/login", false)]
    [InlineData("/", true)]
    [InlineData("/account/password", true)]
    [InlineData("/brands", true)]
    [InlineData("/brands/new", true)]
    [InlineData("/categories", true)]
    [InlineData("/categories/new", true)]
    [InlineData("/products", true)]
    [InlineData("/products/new", true)]
    [InlineData("/products/7", true)]
    [InlineData("/orders", true)]
    [InlineData("/orders/1", true)]
    [InlineData("/orders/2", true)]
    [InlineData("/trendyol/listings", true)]
    [InlineData("/trendyol/brands", true)]
    [InlineData("/trendyol/brands/1", true)]
    [InlineData("/trendyol/categories", true)]
    [InlineData("/trendyol/categories/11", true)]
    [InlineData("/trendyol/categories/11/attributes?all=true", true)]
    [InlineData("/trendyol/batches/batch-1", true)]
    [InlineData("/sync", true)]
    [InlineData("/sync/logs", true)]
    [InlineData("/sync/outbox/Inventory", true)]
    public async Task Pages_have_no_inline_scripts_or_styles(string path, bool signedIn)
    {
        await using var factory = new PanelFactory(api: SyncApi.Register(TrendyolApi.Register(OrderApi.Register(CatalogApi.Create()))));
        var client = factory.CreatePanelClient();
        if (signedIn)
            await PanelFactory.LoginAsync(client, Ct);

        var html = await client.GetStringAsync(path, Ct);
        var document = new HtmlParser().ParseDocument(html);

        document.QuerySelectorAll("script").ShouldAllBe(s => s.HasAttribute("src"));
        document.QuerySelectorAll("[onclick], [onsubmit], [onchange]").ShouldBeEmpty();
        document.QuerySelectorAll("style, [style]").ShouldBeEmpty();
    }

    [Fact]
    public async Task Unknown_page_renders_not_found_view()
    {
        await using var factory = new PanelFactory();
        var client = factory.CreatePanelClient();
        await PanelFactory.LoginAsync(client, Ct);

        var response = await client.GetAsync("/olmayan-sayfa", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var document = new HtmlParser().ParseDocument(await response.Content.ReadAsStringAsync(Ct));
        document.QuerySelector("h1")!.TextContent.ShouldBe("Aradığınız sayfa bulunamadı.");
    }

    [Fact]
    public async Task Post_without_antiforgery_token_is_rejected()
    {
        await using var factory = new PanelFactory();

        var response = await factory.CreatePanelClient().PostAsync("/account/login",
            new FormUrlEncodedContent([new("UserName", "admin"), new("Password", "Parola123!")]), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        factory.Api.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task Api_clients_use_configured_base_address()
    {
        await using var factory = new PanelFactory(new() { ["Api:BaseUrl"] = "http://api:8080" });
        _ = factory.Server;

        var client = factory.Services.GetRequiredService<IHttpClientFactory>().CreateClient(typeof(IOrdersClient).Name);
        var orders = factory.Services.GetRequiredService<IOrdersClient>();

        orders.ShouldBeOfType<OrdersClient>();
        client.BaseAddress.ShouldBe(new Uri("http://api:8080/"));
    }

    [Fact]
    public async Task Api_clients_keep_error_bodies_for_problem_mapping()
    {
        await using var factory = new PanelFactory();
        _ = factory.Server;
        using var scope = factory.Services.CreateScope();

        var clients = typeof(IOrdersClient).Assembly.GetTypes()
            .Where(t => t.IsInterface && t.Namespace == typeof(IOrdersClient).Namespace)
            .Select(t => scope.ServiceProvider.GetService(t))
            .OfType<object>()
            .ToList();

        clients.Count.ShouldBeGreaterThan(5);
        clients.ShouldAllBe(c => (bool)c.GetType().GetProperty("ReadResponseAsString")!.GetValue(c)!);
    }

    [Fact]
    public void Missing_api_base_url_stops_startup()
    {
        using var factory = new PanelFactory(new() { ["Api:BaseUrl"] = "" });

        Should.Throw<OptionsValidationException>(() => factory.Server);
    }
}
