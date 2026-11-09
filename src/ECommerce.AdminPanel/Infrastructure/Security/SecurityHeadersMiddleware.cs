namespace ECommerce.AdminPanel.Infrastructure;

/// <summary>
/// Panel cevaplarına güvenlik başlıklarını ekler. Inline script ve stil yoktur; tüm statik dosyalar panelden sunulur.
/// Ürün görselleri API'nin medya adresinden geldiği için <c>Media:PublicBaseUrl</c> img-src'ye eklenir.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private readonly string contentSecurityPolicy = BuildPolicy(configuration["Media:PublicBaseUrl"]);

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers.ContentSecurityPolicy = contentSecurityPolicy;
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers["Referrer-Policy"] = "same-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        return next(context);
    }

    internal static string BuildPolicy(string? mediaBaseUrl)
    {
        var imageSources = "'self' data:";
        if (Uri.TryCreate(mediaBaseUrl, UriKind.Absolute, out var media))
            imageSources += " " + media.GetLeftPart(UriPartial.Authority);

        return "default-src 'self'; script-src 'self'; style-src 'self'; " +
               $"img-src {imageSources}; font-src 'self'; connect-src 'self'; " +
               "form-action 'self'; frame-ancestors 'none'; base-uri 'self'; object-src 'none'";
    }
}
