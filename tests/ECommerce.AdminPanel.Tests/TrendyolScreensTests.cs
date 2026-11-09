using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System.Net;
using System.Text.Json;

namespace ECommerce.AdminPanel.Tests;

public class TrendyolScreensTests
{
    private const string Base = TrendyolApi.Base;
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<(PanelFactory Factory, HttpClient Client, FakeApi Api)> SignedInAsync(Func<FakeApi, FakeApi>? configure = null)
    {
        var api = TrendyolApi.Register(CatalogApi.Create());
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

    private static JsonElement Body(FakeApi api, string path) => JsonDocument.Parse(api.To(path).Single(r => r.Method != HttpMethod.Get).Body!).RootElement;

    private static KeyValuePair<string, string> Field(string name, string value) => new(name, value);

    [Fact]
    public async Task Brand_mapping_searches_with_brand_name_and_saves_selected_result()
    {
        var (factory, client, api) = await SignedInAsync(a => a.On("PUT", $"{Base}/mappings/brands/2",
            _ => FakeApi.Json(HttpStatusCode.OK, new { brandId = 2, externalBrandId = 12L, externalBrandName = "Zeytin Moda" })));
        await using var _ = factory;

        var list = await PageAsync(client, "/trendyol/brands");
        list.QuerySelectorAll("tbody tr").Select(r => r.QuerySelector("td")!.TextContent).ShouldBe(["Acme", "Çiçek", "Zeytin"]);
        list.QuerySelectorAll(".badge.text-bg-warning").Length.ShouldBe(2);

        var search = await PageAsync(client, "/trendyol/brands/2");
        Uri.UnescapeDataString(api.To($"{Base}/brands").Single().Query).ShouldBe("?name=Zeytin");
        search.QuerySelectorAll("li.list-group-item").Length.ShouldBe(2);

        var response = await PanelFactory.PostFormAsync(client, "/trendyol/brands/2",
            new() { ["externalId"] = "12", ["externalName"] = "Zeytin Moda" }, Ct, "/trendyol/brands/2");

        response.Headers.Location!.OriginalString.ShouldBe("/trendyol/brands");
        Body(api, $"{Base}/mappings/brands/2").GetProperty("externalBrandId").GetInt64().ShouldBe(12);
    }

    [Fact]
    public async Task Selecting_a_trendyol_category_leads_to_attribute_mapping()
    {
        var (factory, client, api) = await SignedInAsync(a => a.On("PUT", $"{Base}/mappings/categories/20",
            _ => FakeApi.Json(HttpStatusCode.OK, new { categoryId = 20, externalCategoryId = 411L, externalCategoryName = "Tişört", attributes = Array.Empty<object>() })));
        await using var _ = factory;

        var search = await PageAsync(client, "/trendyol/categories/20?q=tişört");
        search.QuerySelector("li.list-group-item .small")!.TextContent.ShouldBe("Giyim > Kadın > Tişört");

        var response = await PanelFactory.PostFormAsync(client, "/trendyol/categories/20?q=tişört",
            new() { ["externalId"] = "411", ["externalName"] = "Tişört" }, Ct);

        response.Headers.Location!.OriginalString.ShouldBe("/trendyol/categories/20/attributes");
        Body(api, $"{Base}/mappings/categories/20").GetProperty("externalCategoryId").GetInt64().ShouldBe(411);
    }

    [Fact]
    public async Task Attribute_page_shows_required_variant_and_mapped_attributes_with_existing_values()
    {
        var (factory, client, _) = await SignedInAsync();
        await using var __ = factory;

        var page = await PageAsync(client, "/trendyol/categories/11/attributes");

        page.QuerySelectorAll("fieldset legend").Select(l => l.ChildNodes.First().TextContent.Trim()).ShouldBe(["Beden", "Renk"]);
        page.QuerySelector("a[href$='all=true']")!.TextContent.ShouldContain("2 özelliği de göster");
        page.QuerySelector<IHtmlInputElement>("input[name='rows[0].LocalAttributeName']")!.Value.ShouldBe("Beden");
        page.QuerySelector<IHtmlInputElement>("input[name='rows[0].Values[0].LocalValue']")!.Value.ShouldBe("M");
        page.QuerySelector<IHtmlInputElement>("input[name='rows[0].Values[0].Value']")!.Value.ShouldBe("M");
        page.QuerySelectorAll("datalist#values-47 option").Select(o => o.GetAttribute("value")).ShouldBe(["Mavi", "Kırmızı"]);
    }

    [Fact]
    public async Task Saving_resolves_value_names_to_ids_and_skips_empty_rows()
    {
        var (factory, client, api) = await SignedInAsync(a => a.On("PUT", $"{Base}/mappings/categories/11/attributes",
            _ => FakeApi.Json(HttpStatusCode.OK, new { categoryId = 11, externalCategoryId = 411L, externalCategoryName = "Tişört", attributes = Array.Empty<object>() })));
        await using var _ = factory;

        var response = await PanelFactory.PostFormPairsAsync(client, "/trendyol/categories/11/attributes?all=False",
        [
            Field("rows[0].ExternalAttributeId", "338"), Field("rows[0].LocalAttributeName", "Beden"), Field("rows[0].DefaultValue", ""),
            Field("rows[0].Values[0].LocalValue", "M"), Field("rows[0].Values[0].Value", "m"),
            Field("rows[0].Values[1].LocalValue", "S"), Field("rows[0].Values[1].Value", "S"),
            Field("rows[0].Values[2].LocalValue", ""), Field("rows[0].Values[2].Value", ""),
            Field("rows[1].ExternalAttributeId", "47"), Field("rows[1].LocalAttributeName", ""), Field("rows[1].DefaultValue", "kırmızı")
        ], Ct, "/trendyol/categories/11/attributes");

        response.Headers.Location!.OriginalString.ShouldStartWith("/trendyol/categories/11/attributes");
        var attributes = Body(api, $"{Base}/mappings/categories/11/attributes").GetProperty("attributes").EnumerateArray().ToList();
        attributes.Count.ShouldBe(2);
        attributes[0].GetProperty("localAttributeName").GetString().ShouldBe("Beden");
        attributes[0].GetProperty("values").EnumerateArray().Select(v => (v.GetProperty("localValue").GetString(), v.GetProperty("valueId").GetInt64()))
            .ShouldBe([("M", 2002L), ("S", 2001L)]);
        attributes[1].GetProperty("localAttributeName").ValueKind.ShouldBe(JsonValueKind.Null);
        attributes[1].GetProperty("defaultValueId").GetInt64().ShouldBe(2);
    }

    [Fact]
    public async Task Unknown_value_for_list_only_attribute_is_rejected_but_custom_attribute_accepts_free_text()
    {
        var (factory, client, api) = await SignedInAsync(a => a.On("PUT", $"{Base}/mappings/categories/11/attributes",
            _ => FakeApi.Json(HttpStatusCode.OK, new { categoryId = 11, externalCategoryId = 411L, externalCategoryName = "Tişört", attributes = Array.Empty<object>() })));
        await using var _ = factory;

        var rejected = await PanelFactory.PostFormPairsAsync(client, "/trendyol/categories/11/attributes?all=False",
            [Field("rows[0].ExternalAttributeId", "338"), Field("rows[1].ExternalAttributeId", "47"), Field("rows[1].DefaultValue", "Turkuaz")],
            Ct, "/trendyol/categories/11/attributes");

        (await ParseAsync(rejected)).QuerySelector("[data-valmsg-for='Rows[1].DefaultValue']")!.TextContent
            .ShouldBe("\"Turkuaz\" Renk için Trendyol değer listesinde yok; listeden seçin.");
        api.To($"{Base}/mappings/categories/11/attributes").ShouldBeEmpty();

        await PanelFactory.PostFormPairsAsync(client, "/trendyol/categories/11/attributes?all=True",
            // Tümü açıkken sıra: Beden, Renk (zorunlu), Desen, Materyal (alfabetik).
            [Field("rows[0].ExternalAttributeId", "338"), Field("rows[1].ExternalAttributeId", "47"), Field("rows[2].ExternalAttributeId", "99"),
             Field("rows[3].ExternalAttributeId", "14"), Field("rows[3].DefaultValue", " Pamuk ")], Ct, "/trendyol/categories/11/attributes?all=true");

        var custom = Body(api, $"{Base}/mappings/categories/11/attributes").GetProperty("attributes")[0];
        custom.GetProperty("defaultValueId").ValueKind.ShouldBe(JsonValueKind.Null);
        custom.GetProperty("defaultCustomValue").GetString().ShouldBe("Pamuk");
    }

    [Fact]
    public async Task Product_can_be_sent_to_trendyol_and_api_rejection_reason_is_shown()
    {
        var (factory, client, api) = await SignedInAsync(a => a.On("POST", $"{Base}/listings/{CatalogApi.ProductId}",
            _ => FakeApi.Json(HttpStatusCode.BadRequest, new { title = "İstek pazaryeri tarafından reddedildi.", status = 400, errors = new[] { "Renk zorunlu özelliği eşlenmemiş." } })));
        await using var _ = factory;

        var page = await PageAsync(client, $"/products/{CatalogApi.ProductId}");
        page.QuerySelector("#trendyol-title ~ p")!.TextContent.ShouldContain("henüz Trendyol'a gönderilmedi");

        await PanelFactory.PostFormAsync(client, $"/products/{CatalogApi.ProductId}/trendyol", new() { ["deliveryDuration"] = "2" }, Ct, $"/products/{CatalogApi.ProductId}");

        Body(api, $"{Base}/listings/{CatalogApi.ProductId}").GetProperty("deliveryDuration").GetInt32().ShouldBe(2);
        (await PageAsync(client, $"/products/{CatalogApi.ProductId}")).QuerySelector(".alert-danger")!.TextContent
            .ShouldBe("İstek pazaryeri tarafından reddedildi. Renk zorunlu özelliği eşlenmemiş.");
    }

    [Fact]
    public async Task Live_product_cannot_be_sent_again()
    {
        var (factory, client, _) = await SignedInAsync(a => a.On("GET", $"{Base}/listings/{CatalogApi.ProductId}",
            _ => FakeApi.Json(HttpStatusCode.OK, TrendyolApi.Listing("Live"))));
        await using var __ = factory;

        var page = await PageAsync(client, $"/products/{CatalogApi.ProductId}");

        page.QuerySelector("form[action$='/trendyol']").ShouldBeNull();
        page.QuerySelector("#trendyol-title ~ p .badge")!.TextContent.ShouldBe("Satışta");
    }

    [Fact]
    public async Task Listings_are_filtered_by_status_and_batch_failures_are_listed()
    {
        var (factory, client, api) = await SignedInAsync(a => a.On("POST", $"{Base}/sync/listings",
            _ => FakeApi.Json(HttpStatusCode.OK, new { batches = 1, updated = 3 })));
        await using var _ = factory;

        var listings = await PageAsync(client, "/trendyol/listings?status=Rejected");
        api.To($"{Base}/listings").Single().Query.ShouldBe("?status=Rejected&page=0&size=20");
        listings.QuerySelector("tbody .badge")!.TextContent.ShouldBe("Reddedildi");

        await PanelFactory.PostFormAsync(client, "/trendyol/listings/refresh?status=Rejected", [], Ct, "/trendyol/listings?status=Rejected");
        (await PageAsync(client, "/trendyol/listings")).QuerySelector(".alert-success")!.TextContent.ShouldBe("1 toplu işlem ve 3 ürün durumu güncellendi.");

        var batch = await PageAsync(client, "/trendyol/batches/batch-1");
        batch.QuerySelectorAll("tbody li").Select(li => li.TextContent).ShouldBe(["Görsel kalitesi yetersiz", "Açıklama çok kısa"]);
    }
}
