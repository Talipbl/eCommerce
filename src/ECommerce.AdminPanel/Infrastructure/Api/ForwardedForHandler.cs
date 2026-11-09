namespace ECommerce.AdminPanel.Infrastructure;

/// <summary>
/// API giriş limitini IP'ye göre uyguladığı için gerçek istemci IP'sini <c>X-Forwarded-For</c> ile iletir.
/// Tarayıcıdan gelen değer kullanılmaz; panelin gördüğü bağlantı adresi yazılır.
/// </summary>
internal sealed class ForwardedForHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    public const string HeaderName = "X-Forwarded-For";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Remove(HeaderName);
        if (httpContextAccessor.HttpContext?.Connection.RemoteIpAddress is { } address)
        {
            var ip = address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
            request.Headers.TryAddWithoutValidation(HeaderName, ip.ToString());
        }

        return base.SendAsync(request, cancellationToken);
    }
}
