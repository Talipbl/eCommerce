using ECommerce.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using WireMock;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace ECommerce.IntegrationTests;

/// <summary>
/// Faz 4.3 kabul senaryosu: gerçek Trendyol client'ı WireMock'a, API gerçek SQL Server'a (Testcontainers) bağlı.
/// </summary>
[Collection(SqlServerCollection.Name)]
public class MarketplaceAcceptanceTests : IAsyncLifetime
{
    private const string Base = "/api/v1/marketplaces/trendyol";
    private const string Products = "/integration/product/sellers/1/products";
    private const string PriceInventory = "/integration/inventory/sellers/1/products/price-and-inventory";
    private const string Orders = "/integration/order/sellers/1";
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00];

    private readonly SqlServerFixture _database;
    private readonly string _suffix = Guid.NewGuid().ToString("N")[..10];
    private readonly string _mediaRoot = Path.Combine(Path.GetTempPath(), "ecommerce-media-" + Guid.NewGuid().ToString("N"));
    private WireMockServer _trendyol = null!;
    private ApiFactory _factory = null!;
    private HttpClient _admin = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public MarketplaceAcceptanceTests(SqlServerFixture database) => _database = database;

    public ValueTask InitializeAsync()
    {
        Assert.SkipWhen(_database.UnavailableReason is not null, _database.UnavailableReason ?? string.Empty);

        _trendyol = WireMockServer.Start();
        _factory = new ApiFactory(_database.ConnectionString, new Dictionary<string, string?>
        {
            ["Trendyol:BaseUrl"] = _trendyol.Url,
            ["Media:RootPath"] = _mediaRoot,
            ["Media:PublicBaseUrl"] = "https://api.example.com"
        });
        _admin = _factory.CreateAdminClient();

        Respond("GET", "/integration/product/product-categories", 200, """
            {"categories":[{"id":1,"name":"Giyim","parentId":null,"subCategories":[{"id":411,"name":"Tişört","parentId":1,"subCategories":[]}]}]}
            """);
        Respond("GET", "/integration/product/categories/411/attributes", 200, """
            {"id":411,"name":"Tişört","displayName":"Tişört","categoryAttributes":[
              {"allowCustom":true,"attribute":{"id":47,"name":"Renk"},"categoryId":411,"required":true,"varianter":false,"slicer":true,"allowMultipleAttributeValues":false},
              {"allowCustom":false,"attribute":{"id":293,"name":"Beden"},"categoryId":411,"required":true,"varianter":true,"slicer":false,"allowMultipleAttributeValues":false}]}
            """);
        Respond("GET", "/integration/product/categories/411/attributes/293/values", 200, """
            {"totalElements":2,"totalPages":1,"page":0,"size":1000,"content":[{"attributeValueId":2001,"attributeValue":"M"},{"attributeValueId":2002,"attributeValue":"L"}]}
            """);
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_factory is not null)
            await _factory.DisposeAsync();
        _trendyol?.Stop();
        _trendyol?.Dispose();
        if (Directory.Exists(_mediaRoot))
            Directory.Delete(_mediaRoot, recursive: true);
    }

    [Fact]
    public async Task Local_product_is_published_shares_stock_with_trendyol_and_orders_from_both_channels_land_in_one_list()
    {
        // 1) Ürün yerelde oluşturulup eşleme ile Trendyol'a yayınlanıyor.
        RespondWithUniqueBatch(PriceInventory);
        var product = await CreateProductAsync(stock: 10);
        await PublishAndApproveAsync(product);

        var create = JsonNode.Parse(Requests("POST", $"/integration/product/sellers/1/v2/products").Single().Body!)!["items"]![0]!;
        create["barcode"]!.GetValue<string>().ShouldBe(product.Barcode);
        create["categoryId"]!.GetValue<long>().ShouldBe(411);
        var attributes = create["attributes"]!.AsArray();
        attributes.ShouldContain(a => a!["attributeId"]!.GetValue<long>() == 293 && a["attributeValueId"]!.GetValue<long>() == 2001);
        attributes.ShouldContain(a => a!["attributeId"]!.GetValue<long>() == 47 && a["customAttributeValue"]!.GetValue<string>() == "Mavi");

        // 2) Yerel stok değişikliği Trendyol'a gidiyor.
        (await _admin.PutAsJsonAsync($"/api/v1/catalog/products/{product.Id}/variants/{product.VariantId}",
            new { price = 150, stockQuantity = 9, isActive = true, attributes = new[] { new { name = "Beden", value = "M" } } }, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        await PostOkAsync($"{Base}/sync/inventory");
        LastPriceInventoryQuantity(product.Barcode).ShouldBe(9);

        // 3) Siteden ve Trendyol'dan gelen siparişler ortak stoğu düşürüyor ve tek listede görünüyor.
        var webOrderNumber = await PlaceWebOrderAsync(product.VariantId, quantity: 2);
        var packageId = Random.Shared.NextInt64(1_000_000, 9_000_000_000);
        Respond("GET", $"{Orders}/v2/orders", 200, PackagePage(packageId, product.Barcode, quantity: 3));

        var import = await (await PostOkAsync($"{Base}/sync/orders")).Content.ReadFromJsonAsync<JsonElement>(Ct);
        import.GetProperty("imported").GetInt32().ShouldBe(1);
        (await (await PostOkAsync($"{Base}/sync/orders")).Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("imported").GetInt32().ShouldBe(0);

        var query = Requests("GET", $"{Orders}/v2/orders").First().Query!;
        query["orderByField"][0].ShouldBe("PackageLastModifiedDate");
        query["orderByDirection"][0].ShouldBe("ASC");

        var list = await _admin.GetFromJsonAsync<JsonElement>($"/api/v1/orders?search={_suffix}", Ct);
        var listed = list.GetProperty("content").EnumerateArray().ToList();
        listed.Select(o => o.GetProperty("orderNumber").GetString()).ShouldBe([webOrderNumber, $"TY{packageId}"], ignoreOrder: true);
        listed.Select(o => o.GetProperty("channel").GetString()).ShouldBe(["Web", "Trendyol"], ignoreOrder: true);
        (await StockAsync(product)).ShouldBe(4);

        // 4) Yerel Hazırlanıyor/Kargoda Trendyol'a Picking/Invoiced olarak bildiriliyor.
        var trendyolOrderId = listed.Single(o => o.GetProperty("channel").GetString() == "Trendyol").GetProperty("id").GetInt32();
        Respond("PUT", $"{Orders}/shipment-packages/{packageId}", 200);
        await PostOkAsync($"/api/v1/orders/{trendyolOrderId}/prepare");
        await PostOkAsync($"{Base}/sync/order-status");
        (await _admin.PostAsJsonAsync($"/api/v1/orders/{trendyolOrderId}/ship", new { invoiceNumber = "GIB2026000000001" }, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        await PostOkAsync($"{Base}/sync/order-status");

        var statusBodies = Requests("PUT", $"{Orders}/shipment-packages/{packageId}").Select(r => JsonNode.Parse(r.Body!)!).ToList();
        statusBodies.Count.ShouldBe(2);
        JsonNode.DeepEquals(statusBodies[0], JsonNode.Parse("""{"lines":[{"lineId":555,"quantity":3}],"status":"Picking","params":{}}""")).ShouldBeTrue(statusBodies[0].ToJsonString());
        JsonNode.DeepEquals(statusBodies[1], JsonNode.Parse("""{"lines":[{"lineId":555,"quantity":3}],"status":"Invoiced","params":{"invoiceNumber":"GIB2026000000001"}}""")).ShouldBeTrue(statusBodies[1].ToJsonString());

        // Sipariş kaynaklı stok düşüşleri de Trendyol'a yansıyor; senkron logu yönetici ucunda görünüyor.
        await PostOkAsync($"{Base}/sync/inventory");
        LastPriceInventoryQuantity(product.Barcode).ShouldBe(4);
        var logs = await _admin.GetFromJsonAsync<JsonElement>($"/api/v1/marketplaces/sync/logs?marketplace=Trendyol&operation=OrderStatus&succeeded=true&size=200", Ct);
        logs.GetProperty("content").EnumerateArray().Count(l => l.GetProperty("reference").GetString() == packageId.ToString()).ShouldBe(2);
    }

    [Fact]
    public async Task Rejected_stock_update_is_logged_and_can_be_retried_from_the_admin_endpoints()
    {
        var product = await CreateProductAsync(stock: 5);
        await PublishAndApproveAsync(product);
        _trendyol.Given(Request.Create().WithPath(PriceInventory).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(400).WithHeader("Content-Type", "application/json")
                .WithBody("""{"errors":[{"key":"items","message":"Geçersiz barkod"}]}"""));

        (await _admin.PutAsJsonAsync($"/api/v1/catalog/products/{product.Id}/variants/{product.VariantId}",
            new { price = 150, stockQuantity = 3, isActive = true, attributes = new[] { new { name = "Beden", value = "M" } } }, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        await PostOkAsync($"{Base}/sync/inventory");

        var failed = await _admin.GetFromJsonAsync<JsonElement>("/api/v1/marketplaces/sync/outbox/Inventory?status=Failed&size=200", Ct);
        var entry = failed.GetProperty("content").EnumerateArray().First(e => e.GetProperty("reference").GetString() == $"variant:{product.VariantId}");
        entry.GetProperty("error").GetString()!.ShouldContain("Geçersiz barkod");
        var logs = await _admin.GetFromJsonAsync<JsonElement>("/api/v1/marketplaces/sync/logs?operation=PriceInventory&succeeded=false", Ct);
        logs.GetProperty("totalElements").GetInt32().ShouldBeGreaterThanOrEqualTo(1);

        // Trendyol düzelince hatalı kayıt tekrar kuyruğa alınıp gönderiliyor.
        _trendyol.Reset();
        RespondWithUniqueBatch(PriceInventory);
        var retried = await _admin.PostAsync($"/api/v1/marketplaces/sync/outbox/Inventory/{entry.GetProperty("id").GetInt32()}/retry", null, Ct);
        retried.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await _admin.PostAsync($"/api/v1/marketplaces/sync/outbox/Inventory/{entry.GetProperty("id").GetInt32()}/retry", null, Ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        await PostOkAsync($"{Base}/sync/inventory");
        LastPriceInventoryQuantity(product.Barcode).ShouldBe(3);

        var states = await _admin.GetFromJsonAsync<JsonElement>("/api/v1/marketplaces/sync/states", Ct);
        states.EnumerateArray().ShouldContain(s => s.GetProperty("job").GetString() == "PriceInventory");
    }

    private async Task PublishAndApproveAsync(CreatedProduct product)
    {
        var batchId = $"batch-{_suffix}";
        Respond("POST", "/integration/product/sellers/1/v2/products", 200, $$"""{"batchRequestId":"{{batchId}}"}""");
        Respond("GET", $"{Products}/batch-requests/{batchId}", 200, $$"""
            {"batchRequestId":"{{batchId}}","status":"COMPLETED","creationDate":1793491200000,"lastModification":1793491260000,"sourceType":"API",
             "itemCount":1,"failedItemCount":0,"batchRequestType":"ProductV2OnBoarding",
             "items":[{"requestItem":{"barcode":"{{product.Barcode}}"},"status":"SUCCESS","failureReasons":[]}]}
            """);
        Respond("GET", $"{Products}/approved", 200, $$"""
            {"totalElements":1,"totalPages":1,"page":0,"size":50,"nextPageToken":null,"content":[
              {"contentId":9510902,"productMainId":"MODEL-{{_suffix}}","brand":{"id":10,"name":"Acme"},"category":{"id":411,"name":"Tişört"},"title":"Ürün",
               "images":[],"variants":[{"variantId":1,"barcode":"{{product.Barcode}}","onSale":true,"locked":false,"archived":false,
                 "stock":{"quantity":10},"price":{"salePrice":150,"listPrice":150} }]}]}
            """);

        (await _admin.PutAsJsonAsync($"{Base}/mappings/categories/{product.CategoryId}", new { externalCategoryId = 411 }, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await _admin.PutAsJsonAsync($"{Base}/mappings/brands/{product.BrandId}", new { externalBrandId = 10, externalBrandName = "Acme" }, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await _admin.PutAsJsonAsync($"{Base}/mappings/categories/{product.CategoryId}/attributes", new
        {
            attributes = new object[]
            {
                new { externalAttributeId = 47, defaultCustomValue = "Mavi", values = Array.Empty<object>() },
                new { externalAttributeId = 293, localAttributeName = "Beden", values = new[] { new { localValue = "M", valueId = 2001 } } }
            }
        }, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var publish = await _admin.PostAsJsonAsync($"{Base}/listings/{product.Id}", new { }, Ct);
        publish.StatusCode.ShouldBe(HttpStatusCode.Accepted, await publish.Content.ReadAsStringAsync(Ct));

        await PostOkAsync($"{Base}/sync/listings");
        await PostOkAsync($"{Base}/sync/listings");
        (await _admin.GetFromJsonAsync<JsonElement>($"{Base}/listings/{product.Id}", Ct)).GetProperty("status").GetString().ShouldBe("Live");
    }

    private async Task<string> PlaceWebOrderAsync(int variantId, int quantity)
    {
        var register = await _factory.CreateClient().PostAsJsonAsync("/api/v1/customer/auth/register",
            new { email = $"musteri-{_suffix}@example.com", password = "Musteri-Parola1", firstName = "Ayşe", lastName = _suffix }, Ct);
        register.StatusCode.ShouldBe(HttpStatusCode.Created);
        var customer = _factory.CreateClient();
        customer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await register.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("accessToken").GetString());

        var address = await customer.PostAsJsonAsync("/api/v1/customer/addresses",
            new { title = "Ev", fullName = "Ayşe Yılmaz", phone = "05551112233", city = "İstanbul", district = "Kadıköy", addressLine = "Moda Cd. No:1" }, Ct);
        var addressId = (await address.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("id").GetInt32();
        (await customer.PostAsJsonAsync("/api/v1/customer/cart/items", new { variantId, quantity }, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var placed = await customer.PostAsJsonAsync("/api/v1/customer/orders", new { addressId, paymentMethod = "CashOnDelivery" }, Ct);
        placed.StatusCode.ShouldBe(HttpStatusCode.Created, await placed.Content.ReadAsStringAsync(Ct));
        return (await placed.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("orderNumber").GetString()!;
    }

    private string PackagePage(long packageId, string barcode, int quantity) => $$"""
        {"totalElements":1,"totalPages":1,"page":0,"size":200,"content":[
          {"shipmentPackageId":{{packageId}},"orderNumber":"10654411111","status":"Created","orderDate":{{DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeMilliseconds()}},
           "customerFirstName":"Ali","customerLastName":"{{_suffix}}","customerEmail":"pf+abc@trendyolmail.com",
           "cargoProviderName":"Trendyol Express","cargoTrackingNumber":7330000000001,
           "shipmentAddress":{"fullName":"Ali Veli","city":"İstanbul","district":"Kadıköy","fullAddress":"Moda Cd. No:1"},
           "lines":[{"lineId":555,"barcode":"{{barcode}}","productName":"Ürün","quantity":{{quantity}},"lineUnitPrice":150,"lineGrossAmount":{{150 * quantity}},
             "lineTotalDiscount":0,"vatRate":20}]}]}
        """;

    private int LastPriceInventoryQuantity(string barcode) =>
        Requests("POST", PriceInventory)
            .SelectMany(r => JsonNode.Parse(r.Body!)!["items"]!.AsArray())
            .Last(i => i!["barcode"]!.GetValue<string>() == barcode)!["quantity"]!.GetValue<int>();

    private async Task<int> StockAsync(CreatedProduct product) =>
        (await _admin.GetFromJsonAsync<JsonElement>($"/api/v1/catalog/products/{product.Id}", Ct)).GetProperty("variants")[0].GetProperty("stockQuantity").GetInt32();

    private async Task<HttpResponseMessage> PostOkAsync(string url)
    {
        var response = await _admin.PostAsync(url, null, Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(Ct));
        return response;
    }

    private void Respond(string method, string path, int status, string? body = null) =>
        _trendyol.Given(Request.Create().WithPath(path).UsingMethod(method))
            .RespondWith(body is null
                ? Response.Create().WithStatusCode(status)
                : Response.Create().WithStatusCode(status).WithHeader("Content-Type", "application/json").WithBody(body));

    // Trendyol her istek için yeni bir batchRequestId döndürür; tekil index bunu bekliyor.
    private void RespondWithUniqueBatch(string path) =>
        _trendyol.Given(Request.Create().WithPath(path).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
                .WithBody("""{"batchRequestId":"{{Random Type="Guid"}}"}""").WithTransformer());

    private List<IRequestMessage> Requests(string method, string path) =>
        _trendyol.LogEntries.Select(e => e.RequestMessage!).Where(r => r.Method == method && r.Path == path).ToList();

    private sealed record CreatedProduct(int Id, int VariantId, int CategoryId, int BrandId, string Barcode);

    private async Task<CreatedProduct> CreateProductAsync(int stock)
    {
        var id = Guid.NewGuid().ToString("N")[..10];
        var category = await (await _admin.PostAsJsonAsync("/api/v1/catalog/categories", new { name = $"Kategori {id}", sortOrder = 0, isActive = true }, Ct))
            .Content.ReadFromJsonAsync<JsonElement>(Ct);
        var brand = await (await _admin.PostAsJsonAsync("/api/v1/catalog/brands", new { name = $"Marka {id}", isActive = true }, Ct))
            .Content.ReadFromJsonAsync<JsonElement>(Ct);
        var barcode = $"BRK-{id}";
        var created = await _admin.PostAsJsonAsync("/api/v1/catalog/products", new
        {
            name = $"Ürün {id}",
            productCode = $"MODEL-{id}",
            brandId = brand.GetProperty("id").GetInt32(),
            categoryId = category.GetProperty("id").GetInt32(),
            vatRate = 20,
            description = "<p>Pamuk</p>",
            variants = new[] { new { sku = $"SKU-{id}", barcode, price = 150, stockQuantity = stock, attributes = new[] { new { name = "Beden", value = "M" } } } }
        }, Ct);
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        var product = await created.Content.ReadFromJsonAsync<JsonElement>(Ct);
        var productId = product.GetProperty("id").GetInt32();

        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(Png);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(file, "file", "on.png");
        (await _admin.PostAsync($"/api/v1/catalog/products/{productId}/images", form, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await _admin.PostAsync($"/api/v1/catalog/products/{productId}/publish", null, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);

        return new CreatedProduct(productId, product.GetProperty("variants")[0].GetProperty("id").GetInt32(),
            category.GetProperty("id").GetInt32(), brand.GetProperty("id").GetInt32(), barcode);
    }
}
