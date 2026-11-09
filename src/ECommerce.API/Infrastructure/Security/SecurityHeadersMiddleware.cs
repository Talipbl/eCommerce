namespace ECommerce.API.Infrastructure;

public class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers["Referrer-Policy"] = "no-referrer";

        // Scalar arayüzü kendi script'lerini yüklediği için CSP sadece API cevaplarına uygulanır.
        if (!context.Request.Path.StartsWithSegments("/scalar"))
            headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";

        return next(context);
    }
}
