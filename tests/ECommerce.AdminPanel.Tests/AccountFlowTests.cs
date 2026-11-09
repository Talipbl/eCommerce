using AngleSharp.Html.Parser;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace ECommerce.AdminPanel.Tests;

public class AccountFlowTests
{
    private const string ChangePasswordPath = "/api/v1/auth/change-password";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static readonly Dictionary<string, string> NewPasswordForm = new()
    {
        ["CurrentPassword"] = "Parola123!",
        ["NewPassword"] = "YeniParola123!",
        ["ConfirmNewPassword"] = "YeniParola123!"
    };

    private static async Task<string> SummaryErrorsAsync(HttpResponseMessage response) =>
        new HtmlParser().ParseDocument(await response.Content.ReadAsStringAsync(Ct)).QuerySelector(".validation-summary-errors")?.TextContent.Trim() ?? "";

    [Fact]
    public async Task Anonymous_request_redirects_to_login_with_return_url()
    {
        await using var factory = new PanelFactory();

        var response = await factory.CreatePanelClient().GetAsync("/orders?page=2", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.PathAndQuery.ShouldBe("/account/login?ReturnUrl=%2Forders%3Fpage%3D2");
    }

    [Theory]
    [InlineData("/account/login")]
    [InlineData("/css/site.css")]
    [InlineData("/health/live")]
    public async Task Login_page_static_files_and_health_are_anonymous(string path)
    {
        await using var factory = new PanelFactory();

        var response = await factory.CreatePanelClient().GetAsync(path, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_keeps_api_tokens_on_server_and_opens_panel_session()
    {
        await using var factory = new PanelFactory();
        var client = factory.CreatePanelClient();

        var login = await PanelFactory.LoginAsync(client, Ct);

        login.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        login.Headers.Location!.OriginalString.ShouldBe("/");
        var cookies = login.Headers.GetValues("Set-Cookie").ToList();
        cookies.ShouldContain(c => c.StartsWith("admin_session=") && c.Contains("httponly") && c.Contains("secure") && c.Contains("samesite=strict"));
        cookies.ShouldAllBe(c => !c.Contains(FakeApi.AccessToken) && !c.Contains(FakeApi.RefreshToken) && !c.StartsWith("ecommerce_refresh"));

        var dashboard = await client.GetStringAsync("/", Ct);
        dashboard.ShouldContain("admin");
    }

    [Fact]
    public async Task Login_returns_to_requested_local_page_only()
    {
        await using var factory = new PanelFactory();
        var client = factory.CreatePanelClient();
        var form = new Dictionary<string, string> { ["UserName"] = "admin", ["Password"] = "Parola123!" };

        var external = await PanelFactory.PostFormAsync(client, "/account/login?returnUrl=https%3A%2F%2Fevil.test%2F", form, Ct, "/account/login");

        external.Headers.Location!.OriginalString.ShouldBe("/");
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "Kullanıcı adı veya parola hatalı.")]
    [InlineData(HttpStatusCode.TooManyRequests, "Çok fazla deneme yapıldı, lütfen biraz sonra tekrar deneyin.")]
    public async Task Failed_login_shows_api_message_without_session(HttpStatusCode status, string title)
    {
        var api = new FakeApi().On("POST", "/api/v1/auth/login", _ => FakeApi.Problem(status, title));
        await using var factory = new PanelFactory(api: api);

        var response = await PanelFactory.LoginAsync(factory.CreatePanelClient(), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await SummaryErrorsAsync(response)).ShouldBe(title);
        response.Headers.TryGetValues("Set-Cookie", out var cookies).ShouldBeTrue();
        cookies!.ShouldNotContain(c => c.StartsWith("admin_session="));
    }

    [Fact]
    public async Task Logout_revokes_refresh_token_and_ends_session()
    {
        await using var factory = new PanelFactory();
        var client = factory.CreatePanelClient();
        await PanelFactory.LoginAsync(client, Ct);

        var logout = await PanelFactory.PostFormAsync(client, "/account/logout", [], Ct, "/");

        logout.Headers.Location!.OriginalString.ShouldBe("/account/login");
        factory.Api.To("/api/v1/auth/logout").Single().Cookie.ShouldBe($"ecommerce_refresh={FakeApi.RefreshToken}");
        (await client.GetAsync("/", Ct)).StatusCode.ShouldBe(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task Changing_password_sends_bearer_and_signs_out()
    {
        var api = FakeApi.Admin().On("POST", ChangePasswordPath, _ => new HttpResponseMessage(HttpStatusCode.NoContent));
        await using var factory = new PanelFactory(api: api);
        var client = factory.CreatePanelClient();
        await PanelFactory.LoginAsync(client, Ct);

        var response = await PanelFactory.PostFormAsync(client, "/account/password", NewPasswordForm, Ct);

        response.Headers.Location!.OriginalString.ShouldBe("/account/login");
        api.To(ChangePasswordPath).Single().Bearer.ShouldBe(FakeApi.AccessToken);
        var login = await client.GetStringAsync("/account/login", Ct);
        login.ShouldContain("Parolanız değişti");
        (await client.GetAsync("/", Ct)).StatusCode.ShouldBe(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task Password_validation_errors_from_api_go_to_fields()
    {
        var api = FakeApi.Admin().On("POST", ChangePasswordPath, _ => FakeApi.Json(HttpStatusCode.BadRequest, new
        {
            title = "One or more validation errors occurred.",
            status = 400,
            errors = new Dictionary<string, string[]> { ["NewPassword"] = ["Parola en az 10 karakter olmalı."] }
        }));
        await using var factory = new PanelFactory(api: api);
        var client = factory.CreatePanelClient();
        await PanelFactory.LoginAsync(client, Ct);

        var response = await PanelFactory.PostFormAsync(client, "/account/password", NewPasswordForm, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var document = new HtmlParser().ParseDocument(await response.Content.ReadAsStringAsync(Ct));
        document.QuerySelector("[data-valmsg-for=NewPassword]")!.TextContent.ShouldBe("Parola en az 10 karakter olmalı.");
    }

    [Fact]
    public async Task Expired_api_session_redirects_to_login()
    {
        var api = FakeApi.Admin()
            .On("POST", ChangePasswordPath, _ => new HttpResponseMessage(HttpStatusCode.Unauthorized))
            .On("POST", "/api/v1/auth/refresh", _ => FakeApi.Problem(HttpStatusCode.Unauthorized, "Oturumun süresi doldu."));
        await using var factory = new PanelFactory(api: api);
        var client = factory.CreatePanelClient();
        await PanelFactory.LoginAsync(client, Ct);

        var response = await PanelFactory.PostFormAsync(client, "/account/password", NewPasswordForm, Ct);

        response.Headers.Location!.OriginalString.ShouldBe("/account/login");
        api.To("/api/v1/auth/refresh").Single().Cookie.ShouldBe($"ecommerce_refresh={FakeApi.RefreshToken}");
        (await client.GetAsync("/", Ct)).StatusCode.ShouldBe(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task Panel_cookie_without_server_session_is_rejected()
    {
        await using var factory = new PanelFactory();
        var client = factory.CreatePanelClient();
        await PanelFactory.LoginAsync(client, Ct);

        ((MemoryCache)factory.Services.GetRequiredService<IMemoryCache>()).Clear();
        var response = await client.GetAsync("/", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.AbsolutePath.ShouldBe("/account/login");
    }
}
