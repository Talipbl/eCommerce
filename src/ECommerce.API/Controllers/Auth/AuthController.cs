using Asp.Versioning;
using ECommerce.API.Infrastructure;
using ECommerce.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.API.Controllers.Auth;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    public const string RefreshCookieName = "ecommerce_refresh";

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(SecurityRegistration.AuthRateLimitPolicy)]
    [ProducesResponseType<AccessTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        return result.Succeeded ? Issue(result.Value!) : InvalidCredentials();
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(SecurityRegistration.AuthRateLimitPolicy)]
    [ProducesResponseType<AccessTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken) || string.IsNullOrEmpty(refreshToken))
            return Unauthorized401("Oturum bulunamadı.");

        var result = await authService.RefreshAsync(refreshToken, cancellationToken);
        if (result.Succeeded)
            return Issue(result.Value!);

        DeleteRefreshCookie();
        return Unauthorized401("Oturumun süresi doldu, lütfen tekrar giriş yapın.");
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken) && !string.IsNullOrEmpty(refreshToken))
            await authService.LogoutAsync(refreshToken, cancellationToken);

        DeleteRefreshCookie();
        return NoContent();
    }

    [HttpGet("me")]
    [ProducesResponseType<AdminProfile>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var profile = await authService.GetProfileAsync(User.GetAccountId(), cancellationToken);
        return profile is null ? Unauthorized401("Oturum geçersiz.") : Ok(profile);
    }

    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.ChangePasswordAsync(User.GetAccountId(), request, cancellationToken);
        if (!result.Succeeded)
            return Problem(title: "Mevcut parola hatalı.", statusCode: StatusCodes.Status400BadRequest);

        DeleteRefreshCookie();
        return NoContent();
    }

    private IActionResult Issue(AuthTokens tokens)
    {
        Response.Cookies.Append(RefreshCookieName, tokens.RefreshToken, RefreshCookieOptions(tokens.RefreshTokenExpiresAt));
        return Ok(new AccessTokenResponse(tokens.AccessToken, tokens.AccessTokenExpiresAt));
    }

    private void DeleteRefreshCookie() => Response.Cookies.Delete(RefreshCookieName, RefreshCookieOptions(null));

    private CookieOptions RefreshCookieOptions(DateTime? expiresAt) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = Url.Content("~/api/v1/auth"),
        Expires = expiresAt
    };

    private ObjectResult InvalidCredentials() => Unauthorized401("Kullanıcı adı veya parola hatalı.");

    private ObjectResult Unauthorized401(string title) => Problem(title: title, statusCode: StatusCodes.Status401Unauthorized);
}
