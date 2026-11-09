using ECommerce.AdminPanel.Api;
using ECommerce.AdminPanel.Infrastructure;
using ECommerce.AdminPanel.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerce.AdminPanel.Controllers;

[Route("account")]
public class AccountController(IApiAuthClient apiAuth, IAuthClient authClient, AdminSessionStore sessions, ILogger<AccountController> logger)
    : Controller
{
    internal const string NoticeKey = "Notice";

    [AllowAnonymous]
    [HttpGet("login")]
    public IActionResult Login(string? returnUrl)
    {
        if (sessions.Find(User) is not null)
            return RedirectToLocal(returnUrl);

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl, CancellationToken cancellationToken)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
            return View(model);

        ApiTokens tokens;
        try
        {
            tokens = await apiAuth.LoginAsync(model.UserName, model.Password, cancellationToken);
        }
        catch (ApiException exception)
        {
            ModelState.AddApiProblem(exception);
            return View(model);
        }

        var session = sessions.Create(model.UserName, tokens);
        var identity = new ClaimsIdentity(
            [new Claim(AdminSessionStore.SessionIdClaim, session.Id), new Claim(ClaimTypes.Name, model.UserName)],
            CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return RedirectToLocal(returnUrl);
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (sessions.Find(User) is { } session)
        {
            try
            {
                await apiAuth.LogoutAsync(session.Tokens.RefreshToken, CancellationToken.None);
            }
            catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
            {
                // Yerel oturum yine de kapatılır; API'deki refresh token kendi süresi dolunca geçersiz olur.
                logger.LogWarning(exception, "API çıkışı başarısız oldu, yalnızca panel oturumu kapatılıyor.");
            }
        }

        await EndSessionAsync();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("password")]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost("password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await authClient.ChangePasswordAsync(
                new ChangePasswordRequest { CurrentPassword = model.CurrentPassword, NewPassword = model.NewPassword },
                cancellationToken);
        }
        catch (ApiException exception)
        {
            ModelState.AddApiProblem(exception);
            return View(new ChangePasswordViewModel());
        }

        // API parola değişince tüm refresh token'ları iptal ediyor; panel oturumu da kapanır.
        await EndSessionAsync();
        TempData[NoticeKey] = "Parolanız değişti, yeni parolanızla tekrar giriş yapın.";
        return RedirectToAction(nameof(Login));
    }

    private async Task EndSessionAsync()
    {
        if (sessions.Find(User) is { } session)
            sessions.Remove(session.Id);

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private IActionResult RedirectToLocal(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction(nameof(HomeController.Index), "Home");
}
