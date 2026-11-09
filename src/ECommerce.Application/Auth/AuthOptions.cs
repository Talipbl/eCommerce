namespace ECommerce.Application.Auth;

public class AuthOptions
{
    public const string SectionName = "Auth";

    public int MaxFailedLoginAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "ecommerce-api";
    public string Audience { get; set; } = "ecommerce-clients";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;

    public bool HasValidSigningKey => System.Text.Encoding.UTF8.GetByteCount(SigningKey) >= 32;
}
