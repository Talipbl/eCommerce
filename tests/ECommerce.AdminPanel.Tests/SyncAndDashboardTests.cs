using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System.Net;

namespace ECommerce.AdminPanel.Tests;

public class SyncAndDashboardTests
{
    private const string Base = SyncApi.Base;
    private const string Trendyol = TrendyolApi.Base;
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<(PanelFactory Factory, HttpClient Client, FakeApi Api)> SignedInAsync(Func<FakeApi, FakeApi>? configure = null)
    {
        var api = SyncApi.Register(FakeApi.Admin());
        api = configure?.Invoke(api) ?? api;
        var factory = new PanelFactory(api: api);
        var client = factory.CreatePanelClient();
        await PanelFactory.LoginAsync(client, Ct);
        return (factory, client, api);
    }

    private static async Task<IHtmlDocument> PageAsync(HttpClient client, string path) =>
        new HtmlParser().ParseDocument(await client.GetStringAsync(path, Ct));

    private static async Task<string> FlashAfterRunAsync(HttpClient client, string job)
    {
        await PanelFactory.PostFormAsync(client, $"/sync/run/{job}", [], Ct, "/sync");
        var page = await PageAsync(client, "/sync");
        return (page.QuerySelector(".alert-success") ?? page.QuerySelector(".alert-danger"))!.TextContent;
    }

    [Fact]
    public async Task Dashboard_shows_metrics_and_links_to_problems()
    {
        var (factory, client, _) = await SignedInAsync();
        await using var __ = factory;

        var page = await PageAsync(client, "/");

        page.QuerySelector("[data-metric=pending-transfers]")!.TextContent.ShouldBe("2");
        page.QuerySelector("[data-metric=awaiting-action]")!.TextContent.ShouldBe("3 / 1");
        page.QuerySelector("[data-metric=revenue-today]")!.TextContent.ShouldBe("₺1.250,50");
        page.QuerySelector("a[href='/sync/outbox/inventory?status=Failed']")!.TextContent.ShouldBe("3 hatalı fiyat/stok gönderimi");
        page.QuerySelector("#sync-alert-title")!.ParentElement!.TextContent.ShouldContain("Fiyat ve stok gönderimi");
        page.QuerySelector("#stock-title")!.ParentElement!.TextContent.ShouldContain("Toplam 21 varyant");
    }

    [Fact]
    public async Task Dashboard_still_opens_when_summary_fails()
    {
        var (factory, client, _) = await SignedInAsync(a => a.On("GET", "/api/v1/dashboard/summary",
            _ => FakeApi.Problem(HttpStatusCode.InternalServerError, "Beklenmeyen bir hata oluştu.")));
        await using var __ = factory;

        var response = await client.GetAsync("/", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(Ct)).ShouldContain("Özet alınamadı: Beklenmeyen bir hata oluştu.");
    }

    [Fact]
    public async Task Job_states_highlight_pauses_and_errors()
    {
        var (factory, client, _) = await SignedInAsync();
        await using var __ = factory;

        var row = (await PageAsync(client, "/sync")).QuerySelector("tbody tr")!;

        row.ClassList.ShouldContain("table-warning");
        row.TextContent.ShouldContain("Sipariş içe alma");
        row.TextContent.ShouldContain("Too many requests");
    }

    [Fact]
    public async Task Manual_runs_report_counts_errors_and_rate_limit_wait()
    {
        var (factory, client, _) = await SignedInAsync(a => a
            .On("POST", $"{Trendyol}/sync/orders", _ => FakeApi.Json(HttpStatusCode.OK, new { marketplace = "Trendyol", imported = 2, updated = 1, skipped = 0, error = (string?)null }))
            .On("POST", $"{Trendyol}/sync/order-status", _ => FakeApi.Json(HttpStatusCode.OK, new { marketplace = "Trendyol", imported = 0, updated = 0, skipped = 0 }))
            .On("POST", $"{Trendyol}/sync/inventory", _ =>
            {
                var response = FakeApi.Json(HttpStatusCode.ServiceUnavailable, new { title = "Pazaryeri istek limiti aşıldı, lütfen biraz sonra tekrar deneyin.", status = 503, upstreamStatus = 429 });
                response.Headers.RetryAfter = new(TimeSpan.FromSeconds(60));
                return response;
            }));
        await using var __ = factory;

        (await FlashAfterRunAsync(client, "orders")).ShouldBe("Siparişler: 2 yeni, 1 güncellendi, 0 atlandı.");
        (await FlashAfterRunAsync(client, "inventory")).ShouldBe("Pazaryeri istek limiti aşıldı, lütfen biraz sonra tekrar deneyin. Yaklaşık 60 saniye sonra tekrar deneyin.");
        (await PanelFactory.PostFormAsync(client, "/sync/run/bilinmeyen", [], Ct, "/sync")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Log_filters_are_forwarded()
    {
        var (factory, client, api) = await SignedInAsync();
        await using var __ = factory;

        var page = await PageAsync(client, "/sync/logs?operation=PriceInventory&succeeded=false");

        api.To($"{Base}/logs").Single().Query.ShouldBe("?operation=PriceInventory&succeeded=false&page=0&size=50");
        page.QuerySelector("tbody")!.TextContent.ShouldContain("Barkod bulunamadı");
    }

    [Fact]
    public async Task Only_failed_outbox_entries_can_be_retried_and_bulk_retry_reports_count()
    {
        var (factory, client, api) = await SignedInAsync(a => a
            .On("POST", $"{Base}/outbox/Inventory/5/retry", _ => FakeApi.Json(HttpStatusCode.OK, new
            {
                id = 5, kind = "Inventory", reference = "variant:5", action = (string?)null, status = "Pending", attempts = 0,
                nextAttemptAt = (string?)null, processedAt = (string?)null, error = (string?)null, createdDate = "2026-11-09T06:00:00Z"
            }))
            .On("POST", $"{Base}/outbox/Inventory/retry-failed", _ => FakeApi.Json(HttpStatusCode.OK, new { requeued = 4 })));
        await using var __ = factory;

        var page = await PageAsync(client, "/sync/outbox/Inventory");
        page.QuerySelectorAll("tbody form[action*='/retry']").Length.ShouldBe(1);

        await PanelFactory.PostFormAsync(client, "/sync/outbox/inventory/5/retry", [], Ct, "/sync/outbox/Inventory");
        api.To($"{Base}/outbox/Inventory/5/retry").ShouldHaveSingleItem();

        await PanelFactory.PostFormAsync(client, "/sync/outbox/inventory/retry-failed", [], Ct, "/sync/outbox/Inventory");
        (await PageAsync(client, "/sync/outbox/Inventory?status=Failed")).QuerySelector(".alert-success")!.TextContent.ShouldBe("4 hatalı kayıt yeniden kuyruğa alındı.");
    }
}
