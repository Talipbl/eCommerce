using ECommerce.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace ECommerce.IntegrationTests;

public class ApiPipelineTests : IAsyncLifetime
{
    private const string UnreachableDatabase = "Server=127.0.0.1,1;Database=None;User Id=sa;Password=x;TrustServerCertificate=True;Connect Timeout=1";
    private readonly ApiFactory _factory = new(UnreachableDatabase);

    [Fact]
    public async Task Liveness_does_not_depend_on_database()
    {
        var response = await _factory.CreateClient().GetAsync("/health/live", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Unknown_route_for_anonymous_returns_401_problem_without_revealing_routes()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/unknown", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task Unhandled_exception_returns_problem_details_without_stack_trace()
    {
        var response = await _factory.CreateClient().GetAsync("/test/throw", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");

        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(TestContext.Current.CancellationToken);
        problem.ShouldNotBeNull().Title.ShouldBe("Beklenmeyen bir hata oluştu.");
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldNotContain("Test hatası");
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();

    private record ProblemResponse(string Title, int Status);
}
