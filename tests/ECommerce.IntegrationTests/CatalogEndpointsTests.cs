using ECommerce.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ECommerce.IntegrationTests;

[Collection(SqlServerCollection.Name)]
public class CatalogEndpointsTests : IAsyncLifetime
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];

    private readonly SqlServerFixture _database;
    private readonly string _mediaRoot = Path.Combine(Path.GetTempPath(), "ecommerce-media-" + Guid.NewGuid().ToString("N"));
    private readonly string _suffix = Guid.NewGuid().ToString("N")[..8];
    private ApiFactory _factory = null!;
    private HttpClient _admin = null!;
    private HttpClient _anonymous = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public CatalogEndpointsTests(SqlServerFixture database) => _database = database;

    public ValueTask InitializeAsync()
    {
        Assert.SkipWhen(_database.UnavailableReason is not null, _database.UnavailableReason ?? string.Empty);

        _factory = new ApiFactory(_database.ConnectionString, new Dictionary<string, string?>
        {
            ["Media:RootPath"] = _mediaRoot,
            ["Media:PublicBaseUrl"] = "https://cdn.test"
        });
        _admin = _factory.CreateAdminClient();
        _anonymous = _factory.CreateClient();
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
    public async Task Admin_catalog_endpoints_require_token_and_store_is_public()
    {
        (await _anonymous.GetAsync("/api/v1/catalog/products", Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await _anonymous.GetAsync("/api/v1/store/products", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await _anonymous.GetAsync("/api/v1/store/categories", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Product_flow_from_admin_to_storefront_with_image()
    {
        var (categoryId, brandId) = await CreateCategoryAndBrandAsync();
        var product = await CreateProductAsync($"Pamuklu Tişört {_suffix}", categoryId, brandId, $"BRK-{_suffix}");
        var productId = product.GetProperty("id").GetInt32();
        var slug = product.GetProperty("slug").GetString()!;

        (await _anonymous.GetAsync($"/api/v1/store/products/{slug}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(Png);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(file, "file", "on.png");
        form.Add(new StringContent("Ön görünüm"), "altText");
        var upload = await _admin.PostAsync($"/api/v1/catalog/products/{productId}/images", form, Ct);
        upload.StatusCode.ShouldBe(HttpStatusCode.OK);
        var imageUrl = (await upload.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("images")[0].GetProperty("url").GetString()!;
        imageUrl.ShouldStartWith($"https://cdn.test/media/products/{productId}/");

        (await _admin.PostAsync($"/api/v1/catalog/products/{productId}/publish", null, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var detail = await _anonymous.GetFromJsonAsync<JsonElement>($"/api/v1/store/products/{slug}", Ct);
        detail.GetProperty("images")[0].GetString().ShouldBe(imageUrl);
        detail.GetProperty("variants")[0].GetProperty("inStock").GetBoolean().ShouldBeTrue();

        var media = await _anonymous.GetAsync(new Uri(imageUrl).AbsolutePath, Ct);
        media.StatusCode.ShouldBe(HttpStatusCode.OK);
        media.Content.Headers.ContentType!.MediaType.ShouldBe("image/png");
    }

    [Fact]
    public async Task Duplicate_barcode_returns_409_and_invalid_request_returns_400()
    {
        var (categoryId, brandId) = await CreateCategoryAndBrandAsync();
        await CreateProductAsync($"Ürün A {_suffix}", categoryId, brandId, $"DUP-{_suffix}");

        var duplicate = await _admin.PostAsJsonAsync("/api/v1/catalog/products", ProductBody($"Ürün B {_suffix}", categoryId, brandId, $"DUP-{_suffix}", sku: $"SKU-B-{_suffix}"), Ct);
        duplicate.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var invalid = await _admin.PostAsJsonAsync("/api/v1/catalog/products", ProductBody("", categoryId, brandId, "BRK 1"), Ct);
        invalid.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Business_rule_violation_returns_400_with_message()
    {
        var (categoryId, brandId) = await CreateCategoryAndBrandAsync();
        var product = await CreateProductAsync($"Tek Varyant {_suffix}", categoryId, brandId, $"ONE-{_suffix}");
        var productId = product.GetProperty("id").GetInt32();
        var variantId = product.GetProperty("variants")[0].GetProperty("id").GetInt32();
        await _admin.PostAsync($"/api/v1/catalog/products/{productId}/publish", null, Ct);

        var response = await _admin.PutAsJsonAsync($"/api/v1/catalog/products/{productId}/variants/{variantId}",
            new { price = 100, listPrice = (decimal?)null, stockQuantity = 1, isActive = false, attributes = Array.Empty<object>() }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("title").GetString()!.ShouldContain("son aktif varyant");
    }

    [Fact]
    public async Task Category_with_products_cannot_be_deleted()
    {
        var (categoryId, brandId) = await CreateCategoryAndBrandAsync();
        await CreateProductAsync($"Kategorili {_suffix}", categoryId, brandId, $"CAT-{_suffix}");

        var response = await _admin.DeleteAsync($"/api/v1/catalog/categories/{categoryId}", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    private async Task<(int CategoryId, int BrandId)> CreateCategoryAndBrandAsync()
    {
        var category = await _admin.PostAsJsonAsync("/api/v1/catalog/categories",
            new { name = $"Tişört {_suffix}-{Guid.NewGuid():N}", sortOrder = 0, isActive = true }, Ct);
        category.StatusCode.ShouldBe(HttpStatusCode.Created);
        var brand = await _admin.PostAsJsonAsync("/api/v1/catalog/brands", new { name = $"Acme {Guid.NewGuid():N}", isActive = true }, Ct);
        brand.StatusCode.ShouldBe(HttpStatusCode.Created);

        return ((await category.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("id").GetInt32(),
                (await brand.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("id").GetInt32());
    }

    private async Task<JsonElement> CreateProductAsync(string name, int categoryId, int brandId, string barcode)
    {
        var response = await _admin.PostAsJsonAsync("/api/v1/catalog/products", ProductBody(name, categoryId, brandId, barcode), Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
    }

    private static object ProductBody(string name, int categoryId, int brandId, string barcode, string? sku = null) => new
    {
        name,
        productCode = "MODEL-1",
        brandId,
        categoryId,
        vatRate = 20,
        description = "<p>Pamuk</p>",
        variants = new[] { new { sku = sku ?? "SKU-" + barcode, barcode, price = 150, listPrice = 200, stockQuantity = 5, attributes = new[] { new { name = "Beden", value = "M" } } } }
    };
}
