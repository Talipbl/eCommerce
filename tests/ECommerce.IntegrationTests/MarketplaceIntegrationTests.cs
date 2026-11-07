using ECommerce.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Trendyol.Client;
using Trendyol.Client.Catalog;
using Trendyol.Client.Orders;
using Trendyol.Client.Products;

namespace ECommerce.IntegrationTests;

[Collection(SqlServerCollection.Name)]
public class MarketplaceIntegrationTests : IAsyncLifetime
{
    private const string Base = "/api/v1/marketplaces/trendyol";
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00];

    private readonly SqlServerFixture _database;
    private readonly ITrendyolCatalogClient _catalog = Substitute.For<ITrendyolCatalogClient>();
    private readonly ITrendyolProductClient _products = Substitute.For<ITrendyolProductClient>();
    private readonly ITrendyolOrderClient _orders = Substitute.For<ITrendyolOrderClient>();
    private readonly string _mediaRoot = Path.Combine(Path.GetTempPath(), "ecommerce-media-" + Guid.NewGuid().ToString("N"));
    private ApiFactory _factory = null!;
    private HttpClient _admin = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public MarketplaceIntegrationTests(SqlServerFixture database) => _database = database;

    public ValueTask InitializeAsync()
    {
        Assert.SkipWhen(_database.UnavailableReason is not null, _database.UnavailableReason ?? string.Empty);

        _factory = new ApiFactory(_database.ConnectionString, new Dictionary<string, string?>
        {
            ["Media:RootPath"] = _mediaRoot,
            ["Media:PublicBaseUrl"] = "https://api.example.com"
        }, services =>
        {
            services.RemoveAll<ITrendyolCatalogClient>();
            services.RemoveAll<ITrendyolProductClient>();
            services.RemoveAll<ITrendyolOrderClient>();
            services.AddSingleton(_catalog);
            services.AddSingleton(_products);
            services.AddSingleton(_orders);
        });
        _admin = _factory.CreateAdminClient();

        _catalog.GetCategoryTreeAsync(Arg.Any<CancellationToken>()).Returns(TrendyolResponse<TrendyolCategoryTree>.Success(200,
            new TrendyolCategoryTree([new TrendyolCategory(1, "Giyim", null, [new TrendyolCategory(411, "Tişört", 1, [])])])));
        _catalog.GetCategoryAttributesAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(TrendyolResponse<TrendyolCategoryAttributes>.Success(200,
            new TrendyolCategoryAttributes(411, "Tişört", "Tişört", [])));
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_factory is not null)
            await _factory.DisposeAsync();
        if (Directory.Exists(_mediaRoot))
            Directory.Delete(_mediaRoot, recursive: true);
    }

    [Fact]
    public async Task Mapped_product_is_published_to_trendyol_with_public_image_urls()
    {
        var product = await CreateProductAsync(stock: 5);
        var batchId = $"batch-{Guid.NewGuid():N}";
        _products.CreateProductsAsync(Arg.Any<IReadOnlyList<CreateProductItem>>(), Arg.Any<CancellationToken>())
            .Returns(TrendyolResponse<BatchRequestResponse>.Success(200, new BatchRequestResponse(batchId)));

        (await _admin.PutAsJsonAsync($"{Base}/mappings/categories/{product.CategoryId}", new { externalCategoryId = 411 }, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await _admin.PutAsJsonAsync($"{Base}/mappings/brands/{product.BrandId}", new { externalBrandId = 10, externalBrandName = "Acme" }, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var publish = await _admin.PostAsJsonAsync($"{Base}/listings/{product.Id}", new
        {
            productAttributes = new[] { new { attributeId = 47, customValue = "Mavi" } },
            variantAttributes = new[] { new { variantId = product.VariantId, attributes = new[] { new { attributeId = 293, valueId = 2001 } } } },
            deliveryDuration = 2
        }, Ct);

        publish.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        (await publish.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("status").GetString().ShouldBe("Submitted");
        await _products.Received(1).CreateProductsAsync(Arg.Is<IReadOnlyList<CreateProductItem>>(items =>
            items.Single().CategoryId == 411 && items.Single().BrandId == 10 &&
            items.Single().Images.Single().Url.StartsWith($"https://api.example.com/media/products/{product.Id}/")), Arg.Any<CancellationToken>());

        var batch = await _admin.GetFromJsonAsync<JsonElement>($"{Base}/batches/{batchId}", Ct);
        batch.GetProperty("operation").GetString().ShouldBe("ProductCreate");
    }

    [Fact]
    public async Task Unmapped_category_cannot_be_published()
    {
        var product = await CreateProductAsync(stock: 1);

        var response = await _admin.PostAsJsonAsync($"{Base}/listings/{product.Id}", new { productAttributes = Array.Empty<object>(), variantAttributes = Array.Empty<object>() }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Trendyol_orders_are_imported_once_and_decrease_local_stock()
    {
        var product = await CreateProductAsync(stock: 5);
        var packageId = Random.Shared.NextInt64(1_000_000, 9_000_000_000);
        _orders.GetShipmentPackagesAsync(Arg.Any<ShipmentPackageQuery>(), Arg.Any<CancellationToken>())
            .Returns(TrendyolResponse<ShipmentPackagePage>.Success(200, new ShipmentPackagePage(0, 200, 1, 1,
            [
                new ShipmentPackage
                {
                    ShipmentPackageId = packageId,
                    OrderNumber = "10654411111",
                    Status = "Created",
                    OrderDate = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeMilliseconds(),
                    CustomerFirstName = "Ali",
                    CustomerLastName = "Veli",
                    CustomerEmail = "pf+abc@trendyolmail.com",
                    ShipmentAddress = new TrendyolAddress { FullName = "Ali Veli", City = "İstanbul", District = "Kadıköy", FullAddress = "Moda Cd. No:1" },
                    Lines = [new ShipmentLine { LineId = 1, Barcode = product.Barcode, ProductName = "Ürün", Quantity = 2, LineGrossAmount = 300m }]
                }
            ])));

        var first = await _admin.PostAsync($"{Base}/sync/orders", null, Ct);
        var second = await _admin.PostAsync($"{Base}/sync/orders", null, Ct);

        (await first.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("imported").GetInt32().ShouldBe(1);
        (await second.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("imported").GetInt32().ShouldBe(0);

        var list = await _admin.GetFromJsonAsync<JsonElement>($"/api/v1/orders?channel=Trendyol&search=TY{packageId}", Ct);
        list.GetProperty("totalElements").GetInt32().ShouldBe(1);
        list.GetProperty("content")[0].GetProperty("paymentMethod").GetString().ShouldBe("Marketplace");

        var detail = await _admin.GetFromJsonAsync<JsonElement>($"/api/v1/catalog/products/{product.Id}", Ct);
        detail.GetProperty("variants")[0].GetProperty("stockQuantity").GetInt32().ShouldBe(3);
    }

    [Fact]
    public async Task Local_stock_changes_reach_the_inventory_outbox()
    {
        var product = await CreateProductAsync(stock: 5);

        await _admin.PutAsJsonAsync($"/api/v1/catalog/products/{product.Id}/variants/{product.VariantId}",
            new { price = 120, stockQuantity = 9, isActive = true, attributes = Array.Empty<object>() }, Ct);
        var sync = await _admin.PostAsync($"{Base}/sync/inventory", null, Ct);

        (await sync.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("processed").GetInt32().ShouldBeGreaterThanOrEqualTo(1);
        await _products.DidNotReceiveWithAnyArgs().UpdatePriceAndInventoryAsync(default!, Ct);
    }

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
