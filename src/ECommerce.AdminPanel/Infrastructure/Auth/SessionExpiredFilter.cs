using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ECommerce.AdminPanel.Infrastructure;

/// <summary>Oturum sona erdiyse panel cookie'sini siler ve girişe yönlendirir; GET isteklerinde kaldığı sayfaya geri döner.</summary>
internal sealed class SessionExpiredFilter : IAsyncExceptionFilter
{
    public async Task OnExceptionAsync(ExceptionContext context)
    {
        if (context.Exception is not AdminSessionExpiredException)
            return;

        var request = context.HttpContext.Request;
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        var returnUrl = HttpMethods.IsGet(request.Method) ? $"{request.PathBase}{request.Path}{request.QueryString}" : null;
        context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
        context.ExceptionHandled = true;
    }
}
