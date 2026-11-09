using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;

namespace ECommerce.AdminPanel.Infrastructure;

public static class AuthRegistration
{
    public const string SessionCookieName = "admin_session";

    /// <summary>
    /// Panelin cookie oturumunu kurar; cookie yalnızca oturum anahtarını taşır, API token'ları <see cref="AdminSessionStore"/>'dadır.
    /// Giriş sayfası dışındaki her uç oturum ister.
    /// </summary>
    public static IServiceCollection AddAdminAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var dataProtection = services.AddDataProtection().SetApplicationName("ECommerce.AdminPanel");
        // Yayında volume'e yazılır; deploy sonrası oturum ve antiforgery cookie'leri geçerli kalır.
        if (configuration["DataProtection:KeysPath"] is { Length: > 0 } keysPath)
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));

        services.AddMemoryCache();
        services.AddSingleton<AdminSessionStore>();

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = SessionCookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.ExpireTimeSpan = AdminSessionStore.IdleTimeout;
                options.SlidingExpiration = true;
                options.LoginPath = "/account/login";
                options.LogoutPath = "/account/logout";
                options.AccessDeniedPath = "/error/403";
                options.Events.OnValidatePrincipal = RejectWithoutServerSessionAsync;
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return services;
    }

    // Panel yeniden başladıysa ya da oturum silindiyse cookie geçerli olsa bile sunucuda token yoktur.
    private static async Task RejectWithoutServerSessionAsync(CookieValidatePrincipalContext context)
    {
        if (context.HttpContext.RequestServices.GetRequiredService<AdminSessionStore>().Find(context.Principal) is not null)
            return;

        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
