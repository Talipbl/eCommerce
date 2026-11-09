using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System.Net;
using System.Text.Json;

namespace ECommerce.AdminPanel.Tests;

public class OrderScreensTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<(PanelFactory Factory, HttpClient Client, FakeApi Api)> SignedInAsync(Func<FakeApi, FakeApi>? configure = null)
    {
        var api = OrderApi.Register(FakeApi.Admin());
        api = configure?.Invoke(api) ?? api;
        var factory = new PanelFactory(api: api);
        var client = factory.CreatePanelClient();
        await PanelFactory.LoginAsync(client, Ct);
        return (factory, client, api);
    }

    private static async Task<IHtmlDocument> PageAsync(HttpClient client, string path) =>
        new HtmlParser().ParseDocument(await client.GetStringAsync(path, Ct));

    private static async Task<IHtmlDocument> ParseAsync(HttpResponseMessage response) =>
        new HtmlParser().ParseDocument(await response.Content.ReadAsStringAsync(Ct));

    private static string[] Actions(IHtmlDocument page) =>
        page.QuerySelectorAll("#actions-title ~ form button[type=submit]").Select(b => b.TextContent.Trim()).ToArray();

    [Fact]
    public async Task List_forwards_filters_and_shows_istanbul_time_and_turkish_labels()
    {
        var (factory, client, api) = await SignedInAsync();
        await using var _ = factory;

        var page = await PageAsync(client, "/orders?status=Shipped&channel=Trendyol&search=ali&page=1");

        Uri.UnescapeDataString(api.To("/api/v1/orders").Single().Query).ShouldBe("?status=Shipped&channel=Trendyol&search=ali&page=1&size=20");
        var cells = page.QuerySelectorAll("tbody td").Select(td => td.TextContent.Trim()).ToList();
        cells.ShouldContain("09.11.2026 09:00");
        cells.ShouldContain("Trendyol");
        cells.ShouldContain("Pazaryeri");
        cells.ShouldContain("₺2.999,80");
        cells.ShouldContain("Kargoda");
        page.QuerySelector<IHtmlOptionElement>("#status option[value=Shipped]")!.IsSelected.ShouldBeTrue();
    }

    [Fact]
    public async Task Pending_bank_transfer_offers_payment_confirmation_and_cancel_only()
    {
        var (factory, client, _) = await SignedInAsync();
        await using var __ = factory;

        var page = await PageAsync(client, $"/orders/{OrderApi.WebOrderId}");

        Actions(page).ShouldBe(["Ödemeyi onayla", "Siparişi iptal et"]);
        page.QuerySelector("#Ship_CargoProvider").ShouldBeNull();
    }

    [Fact]
    public async Task Trendyol_order_asks_for_invoice_number_and_cannot_be_cancelled_from_panel()
    {
        var (factory, client, _) = await SignedInAsync();
        await using var __ = factory;

        var page = await PageAsync(client, $"/orders/{OrderApi.TrendyolOrderId}");

        Actions(page).ShouldBe(["Kargoya ver"]);
        page.QuerySelector<IHtmlInputElement>("#Ship_CargoProvider")!.Value.ShouldBe("Trendyol Express");
        page.QuerySelector("#Ship_InvoiceNumber").ShouldNotBeNull();
        page.Body!.TextContent.ShouldContain("Trendyol satıcı panelinden iptal edilir");
    }

    [Fact]
    public async Task Shipping_sends_empty_fields_as_null()
    {
        var (factory, client, api) = await SignedInAsync(a => a.On("POST", $"/api/v1/orders/{OrderApi.TrendyolOrderId}/ship",
            _ => FakeApi.Json(HttpStatusCode.OK, OrderApi.Order(OrderApi.TrendyolOrderId, "Trendyol", "Shipped", "Marketplace"))));
        await using var _ = factory;

        var response = await PanelFactory.PostFormAsync(client, $"/orders/{OrderApi.TrendyolOrderId}/ship",
            new() { ["Ship.CargoProvider"] = " ", ["Ship.TrackingNumber"] = "", ["Ship.InvoiceNumber"] = " GIB2026000000001 " }, Ct, $"/orders/{OrderApi.TrendyolOrderId}");

        response.Headers.Location!.OriginalString.ShouldBe($"/orders/{OrderApi.TrendyolOrderId}");
        var body = JsonDocument.Parse(api.To($"/api/v1/orders/{OrderApi.TrendyolOrderId}/ship").Single().Body!).RootElement;
        body.GetProperty("cargoProvider").ValueKind.ShouldBe(JsonValueKind.Null);
        body.GetProperty("trackingNumber").ValueKind.ShouldBe(JsonValueKind.Null);
        body.GetProperty("invoiceNumber").GetString().ShouldBe("GIB2026000000001");
    }

    [Fact]
    public async Task Shipping_rule_error_from_api_is_shown_in_the_ship_form()
    {
        var (factory, client, _) = await SignedInAsync(a => a.On("POST", $"/api/v1/orders/{OrderApi.TrendyolOrderId}/ship",
            _ => FakeApi.Problem(HttpStatusCode.BadRequest, "Pazaryeri siparişi için fatura numarası girilmelidir.")));
        await using var __ = factory;

        var response = await PanelFactory.PostFormAsync(client, $"/orders/{OrderApi.TrendyolOrderId}/ship",
            new() { ["Ship.CargoProvider"] = "Aras", ["Ship.TrackingNumber"] = "", ["Ship.InvoiceNumber"] = "" }, Ct, $"/orders/{OrderApi.TrendyolOrderId}");

        var page = await ParseAsync(response);
        page.QuerySelector("form[action$='/ship'] .validation-summary-errors")!.TextContent.Trim()
            .ShouldBe("Pazaryeri siparişi için fatura numarası girilmelidir.");
        page.QuerySelector<IHtmlInputElement>("#Ship_CargoProvider")!.Value.ShouldBe("Aras");
    }

    [Fact]
    public async Task Cancel_requires_a_reason_before_calling_api()
    {
        var (factory, client, api) = await SignedInAsync();
        await using var _ = factory;

        var response = await PanelFactory.PostFormAsync(client, $"/orders/{OrderApi.WebOrderId}/cancel",
            new() { ["Cancel.Reason"] = "" }, Ct, $"/orders/{OrderApi.WebOrderId}");

        (await ParseAsync(response)).QuerySelector("[data-valmsg-for='Cancel.Reason']")!.TextContent.ShouldBe("İptal sebebi girilmelidir.");
        api.To($"/api/v1/orders/{OrderApi.WebOrderId}/cancel").ShouldBeEmpty();
    }

    [Fact]
    public async Task Failed_payment_confirmation_shows_api_reason()
    {
        var (factory, client, _) = await SignedInAsync(a => a.On("POST", $"/api/v1/orders/{OrderApi.WebOrderId}/confirm-payment",
            _ => FakeApi.Problem(HttpStatusCode.BadRequest, "Yalnızca ödeme bekleyen siparişin ödemesi onaylanabilir.")));
        await using var __ = factory;

        await PanelFactory.PostFormAsync(client, $"/orders/{OrderApi.WebOrderId}/confirm-payment", [], Ct, $"/orders/{OrderApi.WebOrderId}");

        (await PageAsync(client, $"/orders/{OrderApi.WebOrderId}")).QuerySelector(".alert-danger")!.TextContent
            .ShouldBe("Yalnızca ödeme bekleyen siparişin ödemesi onaylanabilir.");
    }
}
