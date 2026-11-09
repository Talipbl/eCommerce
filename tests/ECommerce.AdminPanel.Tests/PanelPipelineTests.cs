using AngleSharp.Html.Parser;
using ECommerce.AdminPanel.Api;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;

namespace ECommerce.AdminPanel.Tests;

public class PanelPipelineTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static HttpClient Client(PanelFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

    [Fact]
    public async Task Responses_include_strict_security_headers()
    {
        await using var factory = new PanelFactory();

        var response = await Client(factory).GetAsync("/", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.GetValues("X-Frame-Options").ShouldContain("DENY");
        response.Headers.GetValues("X-Content-Type-Options").ShouldContain("nosniff");
        var csp = response.Headers.GetValues("Content-Security-Policy").Single();
        csp.ShouldContain("script-src 'self';");
        csp.ShouldContain("img-src 'self' data: https://shop.test;");
        csp.ShouldContain("frame-ancestors 'none'");
        csp.ShouldNotContain("unsafe-inline");
    }

    [Fact]
    public async Task Pages_have_no_inline_scripts_or_styles()
    {
        await using var factory = new PanelFactory();

        var html = await Client(factory).GetStringAsync("/", Ct);
        var document = new HtmlParser().ParseDocument(html);

        document.QuerySelectorAll("script").ShouldAllBe(s => s.HasAttribute("src"));
        document.QuerySelectorAll("style, [style]").ShouldBeEmpty();
    }

    [Fact]
    public async Task Unknown_page_renders_not_found_view()
    {
        await using var factory = new PanelFactory();

        var response = await Client(factory).GetAsync("/olmayan-sayfa", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var document = new HtmlParser().ParseDocument(await response.Content.ReadAsStringAsync(Ct));
        document.QuerySelector("h1")!.TextContent.ShouldBe("Aradığınız sayfa bulunamadı.");
    }

    [Fact]
    public async Task Post_without_antiforgery_token_is_rejected()
    {
        await using var factory = new PanelFactory();

        var response = await Client(factory).PostAsync("/", new FormUrlEncodedContent([]), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
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
    public void Missing_api_base_url_stops_startup()
    {
        using var factory = new PanelFactory(new() { ["Api:BaseUrl"] = "" });

        Should.Throw<OptionsValidationException>(() => factory.Server);
    }
}
