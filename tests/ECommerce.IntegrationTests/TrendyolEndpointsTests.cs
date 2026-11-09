using ECommerce.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Trendyol.Client;
using Trendyol.Client.Catalog;
using Trendyol.Client.Orders;
using Trendyol.Client.Products;

namespace ECommerce.IntegrationTests;

[Collection(SqlServerCollection.Name)]
public class TrendyolEndpointsTests : IAsyncLifetime
{
    private const string Base = "/api/v1/marketplaces/trendyol";

    private readonly SqlServerFixture _database;
    private readonly ITrendyolCatalogClient _catalog = Substitute.For<ITrendyolCatalogClient>();
    private readonly ITrendyolProductClient _products = Substitute.For<ITrendyolProductClient>();
    private readonly ITrendyolOrderClient _orders = Substitute.For<ITrendyolOrderClient>();
    private ApiFactory _factory = null!;
    private HttpClient _admin = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public TrendyolEndpointsTests(SqlServerFixture database) => _database = database;

    public ValueTask InitializeAsync()
    {
        Assert.SkipWhen(_database.UnavailableReason is not null, _database.UnavailableReason ?? string.Empty);

        _factory = new ApiFactory(_database.ConnectionString, configureServices: services =>
        {
            services.RemoveAll<ITrendyolCatalogClient>();
            services.RemoveAll<ITrendyolProductClient>();
            services.RemoveAll<ITrendyolOrderClient>();
            services.AddSingleton(_catalog);
            services.AddSingleton(_products);
            services.AddSingleton(_orders);
        });
        _admin = _factory.CreateAdminClient();
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_factory is not null)
            await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Trendyol_endpoints_require_admin_token()
    {
        var response = await _factory.CreateClient().GetAsync($"{Base}/categories", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Category_search_returns_leaf_paths()
    {
        _catalog.GetCategoryTreeAsync(Arg.Any<CancellationToken>()).Returns(TrendyolResponse<TrendyolCategoryTree>.Success(200,
            new TrendyolCategoryTree([new TrendyolCategory(1, "Giyim", null, [new TrendyolCategory(2, "Tişört", 1, [])])])));

        var result = await _admin.GetFromJsonAsync<JsonElement>($"{Base}/categories/search?term=tişört", Ct);

        result[0].GetProperty("path").GetString().ShouldBe("Giyim > Tişört");
    }

    [Fact]
    public async Task Invalid_product_is_rejected_before_calling_trendyol()
    {
        var response = await _admin.PostAsJsonAsync($"{Base}/products", new
        {
            items = new[] { new { barcode = "BRK 1", title = "", productMainId = "M", brandId = 1, categoryId = 2, quantity = 1, description = "d",
                listPrice = 10, salePrice = 20, vatRate = 18, images = Array.Empty<object>(), attributes = Array.Empty<object>() } }
        }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await _products.DidNotReceiveWithAnyArgs().CreateProductsAsync(default!, Ct);
    }

    [Fact]
    public async Task Created_product_batch_is_accepted_and_trackable()
    {
        var batchId = $"batch-{Guid.NewGuid():N}";
        _products.CreateProductsAsync(Arg.Any<IReadOnlyList<CreateProductItem>>(), Arg.Any<CancellationToken>())
            .Returns(TrendyolResponse<BatchRequestResponse>.Success(200, new BatchRequestResponse(batchId)));

        var response = await _admin.PostAsJsonAsync($"{Base}/products", new
        {
            items = new[] { new { barcode = "BRK-1", title = "Tişört", productMainId = "MODEL-1", brandId = 10, categoryId = 2, quantity = 5,
                description = "<p>Pamuklu</p>", listPrice = 200, salePrice = 150, vatRate = 20,
                images = new[] { new { url = "https://cdn.example.com/a.jpg" } },
                attributes = new[] { new { attributeId = 293, attributeValueId = 1001 } } } }
        }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        response.Headers.Location!.ToString().ShouldEndWith($"/batches/{batchId}");

        var batch = await _admin.GetFromJsonAsync<JsonElement>($"{Base}/batches/{batchId}", Ct);
        batch.GetProperty("operation").GetString().ShouldBe("ProductCreate");
        batch.GetProperty("status").GetString().ShouldBe("Pending");
        batch.GetProperty("itemCount").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task Unknown_batch_returns_404()
    {
        var response = await _admin.GetAsync($"{Base}/batches/olmayan", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Rejected_credentials_on_trendyol_return_502_problem_with_messages()
    {
        _catalog.SearchBrandsAsync("Acme", Arg.Any<CancellationToken>())
            .Returns(TrendyolResponse<IReadOnlyList<TrendyolBrand>>.Failure(401, new TrendyolError(null, [new TrendyolErrorDetail(null, "Unauthorized", null)], "{}")));

        var response = await _admin.GetAsync($"{Base}/brands?name=Acme", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadGateway);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        problem.GetProperty("title").GetString().ShouldBe("Pazaryeri kimlik bilgileri reddedildi.");
        problem.GetProperty("upstreamStatus").GetInt32().ShouldBe(401);
        problem.GetProperty("errors")[0].GetString().ShouldBe("Unauthorized");
    }

    [Fact]
    public async Task Rate_limited_trendyol_returns_503_with_retry_after()
    {
        _catalog.GetCategoryTreeAsync(Arg.Any<CancellationToken>())
            .Returns(TrendyolResponse<TrendyolCategoryTree>.Failure(429, new TrendyolError(null, [], "")));

        var response = await _admin.GetAsync($"{Base}/categories", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        response.Headers.RetryAfter.ShouldNotBeNull();
    }

    [Fact]
    public async Task Order_query_over_14_days_is_rejected()
    {
        var end = DateTimeOffset.UtcNow;
        var start = end.AddDays(-15);

        var response = await _admin.GetAsync($"{Base}/orders/packages?startDate={Uri.EscapeDataString(start.ToString("O"))}&endDate={Uri.EscapeDataString(end.ToString("O"))}", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Invoiced_package_forwards_invoice_number()
    {
        _orders.UpdatePackageStatusAsync(default, default!, Ct).ReturnsForAnyArgs(TrendyolResponse<TrendyolEmpty>.Success(200, null));

        var response = await _admin.PostAsJsonAsync($"{Base}/orders/packages/3330000001/invoiced",
            new { lines = new[] { new { lineId = 555, quantity = 2 } }, invoiceNumber = "EME2026000025208" }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await _orders.Received(1).UpdatePackageStatusAsync(3330000001,
            Arg.Is<UpdatePackageStatusRequest>(r => r.Status == "Invoiced" && r.Params["invoiceNumber"] == "EME2026000025208"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unsupplied_accepts_reason_as_string()
    {
        _orders.ReportUnsuppliedAsync(default, default!, Ct).ReturnsForAnyArgs(TrendyolResponse<TrendyolEmpty>.Success(200, null));

        var response = await _admin.PostAsJsonAsync($"{Base}/orders/packages/10/unsupplied",
            new { lines = new[] { new { lineId = 555, quantity = 1 } }, reason = "OutOfStock" }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await _orders.Received(1).ReportUnsuppliedAsync(10, Arg.Is<UnsuppliedRequest>(r => r.ReasonId == 500), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OpenApi_document_is_not_exposed_outside_development()
    {
        var response = await _factory.CreateClient().GetAsync("/openapi/v1.json", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
