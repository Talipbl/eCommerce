using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Nodes;
using WireMock;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Trendyol.Client.Tests;

/// <summary>
/// Her test için WireMock sunucusu ve gerçek DI kaydıyla (header'lar, resilience) oluşturulmuş client'lar sağlar.
/// </summary>
public sealed class TrendyolServer : IDisposable
{
    public const string SellerId = "1234";
    public const string ApiKey = "test-key";
    public const string ApiSecret = "test-secret";

    private readonly ServiceProvider _provider;

    public WireMockServer Server { get; } = WireMockServer.Start();

    public TrendyolServer()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Trendyol:BaseUrl"] = Server.Url,
                ["Trendyol:SellerId"] = SellerId,
                ["Trendyol:ApiKey"] = ApiKey,
                ["Trendyol:ApiSecret"] = ApiSecret
            })
            .Build();

        _provider = new ServiceCollection().AddTrendyol(configuration).BuildServiceProvider();
    }

    public T Client<T>() where T : notnull => _provider.GetRequiredService<T>();

    public void Respond(string method, string path, int status, string? body = null) =>
        Server.Given(Request.Create().WithPath(path).UsingMethod(method))
            .RespondWith(body is null
                ? Response.Create().WithStatusCode(status)
                : Response.Create().WithStatusCode(status).WithHeader("Content-Type", "application/json").WithBody(body));

    public IRequestMessage SingleRequest() => Server.LogEntries.ShouldHaveSingleItem().RequestMessage!;

    public static void ShouldBeJson(string? actual, string expected) =>
        JsonNode.DeepEquals(JsonNode.Parse(actual!), JsonNode.Parse(expected))
            .ShouldBeTrue($"Beklenen: {expected}{Environment.NewLine}Gelen: {actual}");

    public void Dispose()
    {
        _provider.Dispose();
        Server.Stop();
        Server.Dispose();
    }
}
