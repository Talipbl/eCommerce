using ECommerce.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ECommerce.IntegrationTests;

/// <summary>Dashboard ve yayın listesi sorgularının SQL Server'da çevrilebildiğini doğrular (unit testler InMemory sağlayıcıyla çalışıyor).</summary>
[Collection(SqlServerCollection.Name)]
public class DashboardEndpointsTests : IAsyncLifetime
{
    private readonly SqlServerFixture _database;
    private ApiFactory _factory = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public DashboardEndpointsTests(SqlServerFixture database) => _database = database;

    public ValueTask InitializeAsync()
    {
        Assert.SkipWhen(_database.UnavailableReason is not null, _database.UnavailableReason ?? string.Empty);
        _factory = new ApiFactory(_database.ConnectionString);
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_factory is not null)
            await _factory.DisposeAsync();
    }

    [Theory]
    [InlineData("/api/v1/dashboard/summary")]
    [InlineData("/api/v1/marketplaces/trendyol/listings")]
    public async Task Endpoints_require_admin_token(string path)
    {
        var response = await _factory.CreateClient().GetAsync(path, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Summary_returns_every_order_status_and_low_stock_threshold()
    {
        var response = await _factory.CreateAdminClient().GetAsync("/api/v1/dashboard/summary", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var summary = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        summary.GetProperty("ordersByStatus").EnumerateArray().Select(s => s.GetProperty("status").GetString())
            .ShouldBe(["PendingPayment", "Confirmed", "Preparing", "Shipped", "Delivered", "Cancelled"]);
        summary.GetProperty("lowStock").GetProperty("threshold").GetInt32().ShouldBe(5);
        summary.GetProperty("today").GetProperty("total").GetDecimal().ShouldBeGreaterThanOrEqualTo(0m);
    }

    [Fact]
    public async Task Listings_can_be_filtered_by_status()
    {
        var response = await _factory.CreateAdminClient().GetAsync("/api/v1/marketplaces/trendyol/listings?status=Rejected&size=5", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        page.GetProperty("size").GetInt32().ShouldBe(5);
        page.GetProperty("content").EnumerateArray().ShouldAllBe(l => l.GetProperty("status").GetString() == "Rejected");
    }
}
