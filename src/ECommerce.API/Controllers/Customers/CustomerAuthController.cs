using Asp.Versioning;
using ECommerce.API.Controllers.Auth;
using ECommerce.API.Infrastructure;
using ECommerce.Application.Auth;
using ECommerce.Application.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.API.Controllers.Customers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/customer/auth")]
[Tags("Müşteri - Giriş")]
public class CustomerAuthController(ICustomerAuthService auth) : ControllerBase
{
    public const string RefreshCookieName = "ecommerce_customer_refresh";

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(SecurityRegistration.AuthRateLimitPolicy)]
    [ProducesResponseType<AccessTokenResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Register(RegisterCustomerRequest request, CancellationToken cancellationToken)
    {
        var result = await auth.RegisterAsync(request, cancellationToken);
        return this.ToActionResult(result, tokens =>
        {
            AppendCookie(tokens);
            return StatusCode(StatusCodes.Status201Created, new AccessTokenResponse(tokens.AccessToken, tokens.AccessTokenExpiresAt));
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(SecurityRegistration.AuthRateLimitPolicy)]
    [ProducesResponseType<AccessTokenResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await auth.LoginAsync(request, cancellationToken);
        return result.Succeeded ? Issue(result.Value!) : Unauthorized401("E-posta veya parola hatalı.");
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(SecurityRegistration.AuthRateLimitPolicy)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(RefreshCookieName, out var token) || string.IsNullOrEmpty(token))
            return Unauthorized401("Oturum bulunamadı.");

        var result = await auth.RefreshAsync(token, cancellationToken);
        if (result.Succeeded)
            return Issue(result.Value!);

        DeleteCookie();
        return Unauthorized401("Oturumun süresi doldu, lütfen tekrar giriş yapın.");
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (Request.Cookies.TryGetValue(RefreshCookieName, out var token) && !string.IsNullOrEmpty(token))
            await auth.LogoutAsync(token, cancellationToken);

        DeleteCookie();
        return NoContent();
    }

    [HttpPost("change-password")]
    [Authorize(Policy = SecurityRegistration.CustomerPolicy)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await auth.ChangePasswordAsync(User.GetAccountId(), request, cancellationToken);
        if (!result.Succeeded)
            return Problem(title: "Mevcut parola hatalı.", statusCode: StatusCodes.Status400BadRequest);

        DeleteCookie();
        return NoContent();
    }

    private IActionResult Issue(AuthTokens tokens)
    {
        AppendCookie(tokens);
        return Ok(new AccessTokenResponse(tokens.AccessToken, tokens.AccessTokenExpiresAt));
    }

    private void AppendCookie(AuthTokens tokens) =>
        Response.Cookies.Append(RefreshCookieName, tokens.RefreshToken, CookieOptions(tokens.RefreshTokenExpiresAt));

    private void DeleteCookie() => Response.Cookies.Delete(RefreshCookieName, CookieOptions(null));

    private CookieOptions CookieOptions(DateTime? expiresAt) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = Url.Content("~/api/v1/customer/auth"),
        Expires = expiresAt
    };

    private ObjectResult Unauthorized401(string title) => Problem(title: title, statusCode: StatusCodes.Status401Unauthorized);
}
