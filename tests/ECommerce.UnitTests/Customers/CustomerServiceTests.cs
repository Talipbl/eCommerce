using ECommerce.Application.Auth;
using ECommerce.Application.Common;
using ECommerce.Application.Customers;
using ECommerce.Customers;
using ECommerce.Persistence;
using ECommerce.UnitTests.Auth;
using ECommerce.UnitTests.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using ECommerce.UnitTests.Common;

namespace ECommerce.UnitTests.Customers;

public sealed class CustomerServiceTests : IAsyncDisposable
{
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 7, 10, 0, 0, TimeSpan.Zero));
    private readonly ECommerceContext _context;
    private readonly CustomerAuthService _auth;
    private readonly CustomerAddressService _addresses;
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public CustomerServiceTests()
    {
        _context = PersistenceTestContext.Create(_clock);
        var customers = new EfRepository<Customer>(_context);
        _auth = new CustomerAuthService(customers, new EfRepository<CustomerRefreshToken>(_context), _context,
            new PasswordHasher<Customer>(), new JwtTokenService(Options.Create(AuthTestHarness.Jwt)), Options.Create(AuthTestHarness.Auth), _clock, TestMapper.Instance);
        _addresses = new CustomerAddressService(customers, _context, TestMapper.Instance);
    }

    private static RegisterCustomerRequest Registration(string email = "ali@example.com") =>
        new(email, "Musteri-Parola1", "Ali", "Veli", "05551112233");

    [Fact]
    public async Task Register_creates_customer_and_signs_in()
    {
        var result = await _auth.RegisterAsync(Registration(), Ct);

        result.Value!.AccessToken.ShouldNotBeNullOrWhiteSpace();
        var customer = await _context.Customers.SingleAsync(Ct);
        customer.Email.ShouldBe("ali@example.com");
        customer.PasswordHash.ShouldNotBe("Musteri-Parola1");
        (await _context.CustomerRefreshTokens.CountAsync(Ct)).ShouldBe(1);
    }

    [Fact]
    public async Task Email_must_be_unique_case_insensitively()
    {
        await _auth.RegisterAsync(Registration(), Ct);

        var duplicate = await _auth.RegisterAsync(Registration("ALI@example.com"), Ct);

        duplicate.Error!.Kind.ShouldBe(ErrorKind.Conflict);
    }

    [Fact]
    public async Task Registered_customer_can_login_with_any_email_case()
    {
        await _auth.RegisterAsync(Registration(), Ct);

        var login = await _auth.LoginAsync(new LoginRequest("Ali@Example.com", "Musteri-Parola1"), Ct);

        login.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public async Task Profile_and_addresses_are_managed()
    {
        await _auth.RegisterAsync(Registration(), Ct);
        var customerId = (await _context.Customers.SingleAsync(Ct)).Id;

        var home = (await _addresses.AddAsync(customerId, AddressRequest("Ev"), Ct)).Value!;
        var work = (await _addresses.AddAsync(customerId, AddressRequest("İş"), Ct)).Value!;
        await _addresses.SetDefaultAsync(customerId, work.Id, Ct);
        var list = await _addresses.ListAsync(customerId, Ct);

        home.IsDefault.ShouldBeTrue();
        list.Single(a => a.IsDefault).Title.ShouldBe("İş");

        var updated = await _auth.UpdateProfileAsync(customerId, new UpdateCustomerProfileRequest("Ayşe", "Veli", null), Ct);
        updated.Value!.FirstName.ShouldBe("Ayşe");
    }

    [Fact]
    public async Task Address_of_another_customer_is_not_found()
    {
        await _auth.RegisterAsync(Registration(), Ct);
        await _auth.RegisterAsync(Registration("diger@example.com"), Ct);
        var ids = await _context.Customers.OrderBy(c => c.Id).Select(c => c.Id).ToListAsync(Ct);
        var address = (await _addresses.AddAsync(ids[0], AddressRequest("Ev"), Ct)).Value!;

        var result = await _addresses.DeleteAsync(ids[1], address.Id, Ct);

        result.Error!.Kind.ShouldBe(ErrorKind.NotFound);
    }

    private static SaveAddressRequest AddressRequest(string title) =>
        new(title, "Ali Veli", "05551112233", "İstanbul", "Kadıköy", "Moda Cd. No:1", "34710");

    public ValueTask DisposeAsync() => _context.DisposeAsync();
}
