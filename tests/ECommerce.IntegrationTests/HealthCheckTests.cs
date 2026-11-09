using ECommerce.IntegrationTests.Infrastructure;
using System.Net;

namespace ECommerce.IntegrationTests;

[Collection(SqlServerCollection.Name)]
public class HealthCheckTests(SqlServerFixture database)
{
    [Fact]
    public async Task Readiness_is_healthy_when_database_is_migrated()
    {
        Assert.SkipWhen(database.UnavailableReason is not null, database.UnavailableReason ?? string.Empty);
        await using var factory = new ApiFactory(database.ConnectionString);

        var response = await factory.CreateClient().GetAsync("/health/ready", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Readiness_is_unhealthy_when_database_is_unreachable()
    {
        await using var factory = new ApiFactory("Server=127.0.0.1,1;Database=None;User Id=sa;Password=x;TrustServerCertificate=True;Connect Timeout=1");

        var response = await factory.CreateClient().GetAsync("/health/ready", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }
}
