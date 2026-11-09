using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System.Net;
using System.Text.Json;

namespace ECommerce.AdminPanel.Tests;

public class CatalogScreensTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<(PanelFactory Factory, HttpClient Client)> SignedInAsync(FakeApi api)
    {
        var factory = new PanelFactory(api: api);
        var client = factory.CreatePanelClient();
        await PanelFactory.LoginAsync(client, Ct);
        return (factory, client);
    }

    private static async Task<IHtmlDocument> PageAsync(HttpClient client, string path) =>
        new HtmlParser().ParseDocument(await client.GetStringAsync(path, Ct));

    private static async Task<IHtmlDocument> ParseAsync(HttpResponseMessage response) =>
        new HtmlParser().ParseDocument(await response.Content.ReadAsStringAsync(Ct));

    private static JsonElement Body(RecordedRequest request) => JsonDocument.Parse(request.Body!).RootElement;

    [Fact]
    public async Task Brands_are_listed_in_turkish_alphabetical_order()
    {
        var (factory, client) = await SignedInAsync(CatalogApi.Create());
        await using var _ = factory;

        var page = await PageAsync(client, "/brands");

        page.QuerySelectorAll("tbody tr td:first-child").Select(td => td.TextContent).ShouldBe(["Acme", "Çiçek", "Zeytin"]);
    }

    [Fact]
    public async Task Creating_a_brand_sends_request_and_shows_notice_after_redirect()
    {
        var api = CatalogApi.Create().On("POST", "/api/v1/catalog/brands", _ => FakeApi.Json(HttpStatusCode.Created, CatalogApi.Brand(5, "Yeni")));
        var (factory, client) = await SignedInAsync(api);
        await using var _ = factory;

        var response = await PanelFactory.PostFormAsync(client, "/brands/new", new() { ["Name"] = "Yeni", ["Slug"] = "", ["IsActive"] = "true" }, Ct);

        response.Headers.Location!.OriginalString.ShouldBe("/brands");
        var body = Body(api.To("/api/v1/catalog/brands").Single(r => r.Method == HttpMethod.Post));
        body.GetProperty("name").GetString().ShouldBe("Yeni");
        body.GetProperty("slug").ValueKind.ShouldBe(JsonValueKind.Null);
        (await PageAsync(client, "/brands")).QuerySelector(".alert-success")!.TextContent.ShouldBe("Yeni markası eklendi.");
    }

    [Fact]
    public async Task Brand_validation_error_from_api_is_shown_under_the_field()
    {
        var api = CatalogApi.Create().On("POST", "/api/v1/catalog/brands", _ => FakeApi.Json(HttpStatusCode.BadRequest,
            new { title = "Doğrulama hatası", status = 400, errors = new Dictionary<string, string[]> { ["Slug"] = ["Slug yalnızca küçük harf içerebilir."] } }));
        var (factory, client) = await SignedInAsync(api);
        await using var _ = factory;

        var response = await PanelFactory.PostFormAsync(client, "/brands/new", new() { ["Name"] = "Yeni", ["Slug"] = "BÜYÜK", ["IsActive"] = "true" }, Ct);

        var page = await ParseAsync(response);
        page.QuerySelector("[data-valmsg-for=Slug]")!.TextContent.ShouldBe("Slug yalnızca küçük harf içerebilir.");
        page.QuerySelector<IHtmlInputElement>("#Slug")!.Value.ShouldBe("BÜYÜK");
    }

    [Fact]
    public async Task Category_tree_is_indented_and_delete_conflict_shows_api_reason()
    {
        var api = CatalogApi.Create().On("DELETE", "/api/v1/catalog/categories/10",
            _ => FakeApi.Problem(HttpStatusCode.Conflict, "Alt kategorisi olan kategori silinemez."));
        var (factory, client) = await SignedInAsync(api);
        await using var _ = factory;

        var tree = await PageAsync(client, "/categories");
        tree.QuerySelectorAll(".category-tree li").Select(li => li.ClassList.First(c => c.StartsWith("depth-"))).ShouldBe(["depth-0", "depth-0", "depth-1", "depth-2"]);

        await PanelFactory.PostFormAsync(client, "/categories/10/delete", [], Ct, "/categories");

        (await PageAsync(client, "/categories")).QuerySelector(".alert-danger")!.TextContent.ShouldBe("Alt kategorisi olan kategori silinemez.");
    }

    [Fact]
    public async Task Category_cannot_be_moved_under_itself_or_its_descendants()
    {
        var api = CatalogApi.Create().On("GET", "/api/v1/catalog/categories/11", _ => FakeApi.Json(HttpStatusCode.OK,
            new { id = 11, name = "Tişört", slug = "tisort", parentId = 10, description = "Pamuklu", sortOrder = 0, isActive = true }));
        var (factory, client) = await SignedInAsync(api);
        await using var _ = factory;

        var page = await PageAsync(client, "/categories/11/edit");

        page.QuerySelectorAll("#ParentId option").Select(o => o.GetAttribute("value")).ShouldBe(["", "20", "10"]);
        page.QuerySelector<IHtmlOptionElement>("#ParentId option[value='10']")!.IsSelected.ShouldBeTrue();
        page.QuerySelector("#Description")!.TextContent.ShouldBe("Pamuklu");
    }

    [Fact]
    public async Task Product_list_forwards_filters_and_shows_names_and_turkish_money()
    {
        var api = CatalogApi.Create();
        var (factory, client) = await SignedInAsync(api);
        await using var _ = factory;

        var page = await PageAsync(client, "/products?search=tişört&categoryId=11&brandId=1&isActive=true&page=2");

        Uri.UnescapeDataString(api.To("/api/v1/catalog/products").Single().Query)
            .ShouldBe("?search=tişört&categoryId=11&brandId=1&isActive=true&page=2&size=20");
        var cells = page.QuerySelectorAll("tbody tr td").Select(td => td.TextContent.Trim()).ToList();
        cells.ShouldContain("Acme");
        cells.ShouldContain("Tişört");
        cells.ShouldContain("₺1.499,90");
        cells.ShouldContain("1.250");
    }

    [Fact]
    public async Task Creating_a_product_sends_dot_decimal_price_and_parsed_attributes()
    {
        var api = CatalogApi.Create().On("POST", "/api/v1/catalog/products", _ => FakeApi.Json(HttpStatusCode.Created, CatalogApi.Product()));
        var (factory, client) = await SignedInAsync(api);
        await using var _ = factory;

        var response = await PanelFactory.PostFormAsync(client, "/products/new", new()
        {
            ["Details.Name"] = "Basic Tişört", ["Details.ProductCode"] = "BT-1", ["Details.BrandId"] = "1", ["Details.CategoryId"] = "11",
            ["Details.VatRate"] = "20", ["Variant.Sku"] = "BT-M", ["Variant.Barcode"] = "869000000001", ["Variant.Price"] = "1499.90",
            ["Variant.StockQuantity"] = "3", ["Variant.AttributesText"] = "Beden: M\nRenk:  Mavi "
        }, Ct);

        response.Headers.Location!.OriginalString.ShouldBe($"/products/{CatalogApi.ProductId}");
        var variant = Body(api.To("/api/v1/catalog/products").Single(r => r.Method == HttpMethod.Post)).GetProperty("variants")[0];
        variant.GetProperty("price").GetDecimal().ShouldBe(1499.90m);
        variant.GetProperty("listPrice").ValueKind.ShouldBe(JsonValueKind.Null);
        variant.GetProperty("attributes").EnumerateArray().Select(a => $"{a.GetProperty("name").GetString()}={a.GetProperty("value").GetString()}")
            .ShouldBe(["Beden=M", "Renk=Mavi"]);
    }

    [Fact]
    public async Task Product_create_errors_from_api_land_on_variant_fields()
    {
        var api = CatalogApi.Create().On("POST", "/api/v1/catalog/products", _ => FakeApi.Json(HttpStatusCode.BadRequest, new
        {
            title = "Doğrulama hatası", status = 400,
            errors = new Dictionary<string, string[]> { ["Variants[0].Barcode"] = ["Barkod başka bir üründe kullanılıyor."], ["ProductCode"] = ["Ürün kodu zorunlu."] }
        }));
        var (factory, client) = await SignedInAsync(api);
        await using var _ = factory;

        var response = await PanelFactory.PostFormAsync(client, "/products/new", new()
        {
            ["Details.Name"] = "Basic", ["Details.ProductCode"] = "X", ["Details.BrandId"] = "1", ["Details.CategoryId"] = "11", ["Details.VatRate"] = "20",
            ["Variant.Sku"] = "BT-M", ["Variant.Barcode"] = "1", ["Variant.Price"] = "10", ["Variant.StockQuantity"] = "0"
        }, Ct);

        var page = await ParseAsync(response);
        page.QuerySelector("[data-valmsg-for='Variant.Barcode']")!.TextContent.ShouldBe("Barkod başka bir üründe kullanılıyor.");
        page.QuerySelector("[data-valmsg-for='Details.ProductCode']")!.TextContent.ShouldBe("Ürün kodu zorunlu.");
    }

    [Fact]
    public async Task Malformed_attribute_lines_are_rejected_before_calling_api()
    {
        var api = CatalogApi.Create();
        var (factory, client) = await SignedInAsync(api);
        await using var _ = factory;

        var response = await PanelFactory.PostFormAsync(client, $"/products/{CatalogApi.ProductId}/variants", new()
        {
            ["NewVariant.Sku"] = "BT-L", ["NewVariant.Barcode"] = "2", ["NewVariant.Price"] = "10", ["NewVariant.StockQuantity"] = "1",
            ["NewVariant.AttributesText"] = "Beden L"
        }, Ct, $"/products/{CatalogApi.ProductId}");

        (await ParseAsync(response)).QuerySelector("[data-valmsg-for='NewVariant.AttributesText']")!.TextContent.ShouldBe("Her satır \"Ad: Değer\" biçiminde olmalı.");
        api.To($"/api/v1/catalog/products/{CatalogApi.ProductId}/variants").ShouldBeEmpty();
    }

    [Fact]
    public async Task Edit_page_shows_variant_prices_as_dot_decimals_for_number_inputs()
    {
        var (factory, client) = await SignedInAsync(CatalogApi.Create());
        await using var _ = factory;

        var page = await PageAsync(client, $"/products/{CatalogApi.ProductId}");

        page.QuerySelector<IHtmlInputElement>("input[form='variant-70'][name=Price]")!.Value.ShouldBe("1499.9");
        page.QuerySelector<IHtmlTextAreaElement>("textarea[form='variant-70']")!.Value.ShouldBe("Beden: M");
        page.QuerySelectorAll(".image-grid li img").Select(i => i.GetAttribute("src")).ShouldBe([
            "https://shop.test/media/1.jpg", "https://shop.test/media/2.jpg", "https://shop.test/media/3.jpg"]);
    }

    [Fact]
    public async Task Arrow_button_moves_image_and_sends_new_order()
    {
        var api = CatalogApi.Create().On("PUT", $"/api/v1/catalog/products/{CatalogApi.ProductId}/images/order",
            _ => FakeApi.Json(HttpStatusCode.OK, CatalogApi.Product()));
        var (factory, client) = await SignedInAsync(api);
        await using var _ = factory;

        var response = await PanelFactory.PostFormPairsAsync(client, $"/products/{CatalogApi.ProductId}/images/order",
            [new("imageIds", "1"), new("imageIds", "2"), new("imageIds", "3"), new("move", "3:-1")], Ct, $"/products/{CatalogApi.ProductId}");

        response.Headers.Location!.OriginalString.ShouldBe($"/products/{CatalogApi.ProductId}");
        Body(api.To($"/api/v1/catalog/products/{CatalogApi.ProductId}/images/order").Single())
            .GetProperty("imageIds").EnumerateArray().Select(e => e.GetInt32()).ShouldBe([1, 3, 2]);
    }

    [Fact]
    public async Task Uploaded_image_is_forwarded_as_multipart_file()
    {
        var api = CatalogApi.Create().On("POST", $"/api/v1/catalog/products/{CatalogApi.ProductId}/images", _ => FakeApi.Json(HttpStatusCode.OK, CatalogApi.Product()));
        var (factory, client) = await SignedInAsync(api);
        await using var _ = factory;
        var editPage = await PageAsync(client, $"/products/{CatalogApi.ProductId}");
        var token = editPage.QuerySelector<IHtmlInputElement>("form[enctype='multipart/form-data'] input[name=__RequestVerificationToken]")!.Value;

        using var form = new MultipartFormDataContent
        {
            { new StringContent(token), "__RequestVerificationToken" },
            { new StringContent("Ön görünüm"), "altText" },
            { new ByteArrayContent([0xFF, 0xD8, 0xFF, 0xE0]) { Headers = { ContentType = new("image/jpeg") } }, "file", "on.jpg" }
        };
        var response = await client.PostAsync($"/products/{CatalogApi.ProductId}/images", form, Ct);

        response.Headers.Location!.OriginalString.ShouldBe($"/products/{CatalogApi.ProductId}");
        var upload = api.To($"/api/v1/catalog/products/{CatalogApi.ProductId}/images").Single();
        upload.ContentType.ShouldBe("multipart/form-data");
        upload.Body!.ShouldContain("filename=on.jpg");
        upload.Body!.ShouldContain("Ön görünüm");
    }
}
