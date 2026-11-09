using ECommerce.AdminPanel.Api;

namespace ECommerce.AdminPanel.Infrastructure;

public interface IApiAuthClient
{
    /// <summary>Giriş yapar; başarısızsa API'nin hata cevabıyla <see cref="ApiException"/> fırlatır.</summary>
    Task<ApiTokens> LoginAsync(string userName, string password, CancellationToken cancellationToken);

    /// <summary>Token'ları yeniler; refresh token geçersizse <c>null</c> döner.</summary>
    Task<ApiTokens?> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken);
}
