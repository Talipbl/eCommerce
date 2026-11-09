namespace ECommerce.AdminPanel.Infrastructure;

/// <summary>API'nin verdiği token çifti. Refresh token, API cookie'sindeki ham değerdir ve aynen geri gönderilir.</summary>
public sealed record ApiTokens(string AccessToken, DateTimeOffset AccessTokenExpiresAt, string RefreshToken);
