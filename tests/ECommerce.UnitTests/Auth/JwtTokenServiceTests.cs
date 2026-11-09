using ECommerce.Admin;
using ECommerce.Application.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ECommerce.UnitTests.Auth;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _service = new(Options.Create(AuthTestHarness.Jwt));

    [Fact]
    public async Task Access_token_contains_identity_claims_and_validates()
    {
        var now = DateTime.UtcNow;
        var admin = new DefAdmin { Id = 42, UserName = "admin", Role = AdminRoles.Admin };

        var (token, expiresAt) = _service.CreateAccessToken(admin, now);

        expiresAt.ShouldBe(now.AddMinutes(AuthTestHarness.Jwt.AccessTokenMinutes));
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = AuthTestHarness.Jwt.Issuer,
            ValidAudience = AuthTestHarness.Jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AuthTestHarness.Jwt.SigningKey))
        });

        result.IsValid.ShouldBeTrue();
        result.Claims["sub"].ShouldBe("42");
        result.Claims["name"].ShouldBe("admin");
        result.Claims["role"].ShouldBe("Admin");
    }

    [Fact]
    public void Refresh_tokens_are_random_and_hash_is_deterministic()
    {
        var first = _service.CreateRefreshToken();
        var second = _service.CreateRefreshToken();

        first.ShouldNotBe(second);
        first.Length.ShouldBeGreaterThanOrEqualTo(64);
        _service.HashRefreshToken(first).ShouldBe(_service.HashRefreshToken(first));
        _service.HashRefreshToken(first).Length.ShouldBe(64);
    }
}
