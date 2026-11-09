using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace ECommerce.AdminPanel.Tests;

public sealed class PanelFactory(Dictionary<string, string?>? settings = null, FakeApi? api = null) : WebApplicationFactory<Program>
{
    public FakeApi Api { get; } = api ?? FakeApi.Admin();

    public HttpClient CreatePanelClient() =>
        CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

    /// <summary>Giriş formunu antiforgery token'ıyla gönderir; client panel cookie'sini saklar.</summary>
    public static async Task<HttpResponseMessage> LoginAsync(HttpClient client, CancellationToken cancellationToken, string userName = "admin") =>
        await PostFormAsync(client, "/account/login", new() { ["UserName"] = userName, ["Password"] = "Parola123!" }, cancellationToken);

    public static Task<HttpResponseMessage> PostFormAsync(
        HttpClient client, string path, Dictionary<string, string> fields, CancellationToken cancellationToken, string? formPage = null) =>
        PostFormPairsAsync(client, path, fields, cancellationToken, formPage);

    /// <summary>Aynı adla birden çok değer gönderen formlar için (ör. görsel sırası).</summary>
    public static async Task<HttpResponseMessage> PostFormPairsAsync(
        HttpClient client, string path, IEnumerable<KeyValuePair<string, string>> fields, CancellationToken cancellationToken, string? formPage = null)
    {
        var page = await client.GetStringAsync(formPage ?? path, cancellationToken);
        var token = new HtmlParser().ParseDocument(page)
            .QuerySelectorAll("input[name=__RequestVerificationToken]")
            .Select(input => input.GetAttribute("value"))
            .First(value => !string.IsNullOrEmpty(value))!;

        return await client.PostAsync(path, new FormUrlEncodedContent(fields.Append(new("__RequestVerificationToken", token))), cancellationToken);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("Api:BaseUrl", "http://api.test/");
        builder.UseSetting("Media:PublicBaseUrl", "https://shop.test");
        foreach (var (key, value) in settings ?? [])
            builder.UseSetting(key, value);

        builder.ConfigureTestServices(services => services.ConfigureAll<HttpClientFactoryOptions>(options =>
            options.HttpMessageHandlerBuilderActions.Add(handlers => handlers.PrimaryHandler = Api)));
    }
}
