using System.Net;
using System.Net.Http.Headers;

namespace ECommerce.AdminPanel.Infrastructure;

/// <summary>
/// API isteklerine oturumun access token'ını ekler. Token'ın bitmesine az kaldıysa önce yeniler; 401 gelirse bir kez yenileyip tekrar dener.
/// Yenilenemezse oturum silinir ve <see cref="AdminSessionExpiredException"/> fırlatılır.
/// </summary>
internal sealed class ApiTokenHandler(IHttpContextAccessor httpContextAccessor, AdminSessionStore sessions, IApiAuthClient auth, TimeProvider time)
    : DelegatingHandler
{
    internal static readonly TimeSpan RefreshBeforeExpiry = TimeSpan.FromSeconds(60);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var session = sessions.Find(httpContextAccessor.HttpContext?.User) ?? throw new AdminSessionExpiredException();
        try
        {
            var tokens = session.Tokens;
            var accessToken = tokens.AccessTokenExpiresAt - time.GetUtcNow() < RefreshBeforeExpiry
                ? await session.RefreshAsync(tokens.AccessToken, auth)
                : tokens.AccessToken;

            // Tekrar denemede gövde yeniden gönderilebilsin diye belleğe alınır.
            if (request.Content is not null)
                await request.Content.LoadIntoBufferAsync(cancellationToken);

            var response = await SendWithTokenAsync(request, accessToken, cancellationToken);
            if (response.StatusCode != HttpStatusCode.Unauthorized)
                return response;

            response.Dispose();
            accessToken = await session.RefreshAsync(accessToken, auth);
            response = await SendWithTokenAsync(request, accessToken, cancellationToken);
            if (response.StatusCode != HttpStatusCode.Unauthorized)
                return response;

            response.Dispose();
            throw new AdminSessionExpiredException();
        }
        catch (AdminSessionExpiredException)
        {
            sessions.Remove(session.Id);
            throw;
        }
    }

    private Task<HttpResponseMessage> SendWithTokenAsync(HttpRequestMessage request, string accessToken, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return base.SendAsync(request, cancellationToken);
    }
}
