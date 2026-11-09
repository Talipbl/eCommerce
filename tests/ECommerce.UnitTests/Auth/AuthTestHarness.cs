using ECommerce.Admin;
using ECommerce.Application.Auth;
using ECommerce.Persistence;
using ECommerce.UnitTests.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace ECommerce.UnitTests.Auth;

public sealed class AuthTestHarness : IAsyncDisposable
{
    public const string Password = "Gecerli-Parola-123";

    public static readonly JwtOptions Jwt = new()
    {
        Issuer = "ecommerce-test",
        Audience = "ecommerce-test-clients",
        SigningKey = "test-signing-key-test-signing-key-0123456789",
        AccessTokenMinutes = 15
    };

    public static readonly AuthOptions Auth = new()
    {
        MaxFailedLoginAttempts = 3,
        LockoutMinutes = 15,
        RefreshTokenDays = 7
    };

    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 11, 6, 12, 0, 0, TimeSpan.Zero));
    public ECommerceContext Context { get; }
    public PasswordHasher<DefAdmin> Hasher { get; } = new();
    public JwtTokenService Tokens { get; }
    public AuthService Service { get; }

    public AuthTestHarness()
    {
        Context = PersistenceTestContext.Create(Clock);
        Tokens = new JwtTokenService(Options.Create(Jwt));
        Service = new AuthService(
            new EfRepository<DefAdmin>(Context),
            new EfRepository<AdminRefreshToken>(Context),
            Context,
            Hasher,
            Tokens,
            Options.Create(Auth),
            Clock);
    }

    public async Task<DefAdmin> AddAdminAsync(string userName = "admin", bool isActive = true)
    {
        var admin = new DefAdmin
        {
            UserName = userName,
            FirstName = "Ada",
            LastName = "Yönetici",
            MailAdress = $"{userName}@example.com",
            IsActive = isActive
        };
        admin.PasswordHash = Hasher.HashPassword(admin, Password);
        Context.DefAdmins.Add(admin);
        await Context.SaveChangesAsync();
        return admin;
    }

    public ValueTask DisposeAsync() => Context.DisposeAsync();
}
