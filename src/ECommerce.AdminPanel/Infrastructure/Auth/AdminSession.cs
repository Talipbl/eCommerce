namespace ECommerce.AdminPanel.Infrastructure;

/// <summary>Sunucuda tutulan yönetici oturumu; tarayıcı yalnızca <see cref="Id"/>'yi şifreli cookie içinde taşır.</summary>
public sealed class AdminSession(string id, string userName, ApiTokens tokens)
{
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private volatile ApiTokens _tokens = tokens;
    private volatile bool _expired;

    public string Id { get; } = id;
    public string UserName { get; } = userName;
    public ApiTokens Tokens => _tokens;

    /// <summary>
    /// <paramref name="staleAccessToken"/> hâlâ geçerli token ise yeniler, başka bir istek zaten yenilediyse yeni token'ı döner.
    /// API döndürülmüş refresh token'ın tekrar kullanımını çalınma sayıp tüm oturumları kapattığı için yenileme tek seferde bir istekle yapılır.
    /// </summary>
    internal async Task<string> RefreshAsync(string staleAccessToken, IApiAuthClient auth)
    {
        await _refreshLock.WaitAsync();
        try
        {
            if (_expired)
                throw new AdminSessionExpiredException();
            if (_tokens.AccessToken != staleAccessToken)
                return _tokens.AccessToken;

            // İstek iptal edilse de yenileme tamamlanmalı; aksi halde API'nin döndürdüğü yeni refresh token kaybolur.
            var refreshed = await auth.RefreshAsync(_tokens.RefreshToken, CancellationToken.None);
            if (refreshed is null)
            {
                _expired = true;
                throw new AdminSessionExpiredException();
            }

            _tokens = refreshed;
            return refreshed.AccessToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}
