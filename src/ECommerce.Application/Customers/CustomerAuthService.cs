using ECommerce.Application.Auth;
using ECommerce.Application.Common;
using ECommerce.Core.Persistence;
using ECommerce.Customers;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Linq.Expressions;

namespace ECommerce.Application.Customers;

public class CustomerAuthService(
    IRepository<Customer> customers,
    IRepository<CustomerRefreshToken> refreshTokens,
    IUnitOfWork unitOfWork,
    IPasswordHasher<Customer> passwordHasher,
    ITokenService tokenService,
    IOptions<AuthOptions> options,
    TimeProvider clock,
    IMapper mapper)
    : AccountAuthService<Customer, CustomerRefreshToken>(customers, refreshTokens, unitOfWork, passwordHasher, tokenService, options, clock), ICustomerAuthService
{
    public async Task<Result<AuthTokens>> RegisterAsync(RegisterCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var email = Customer.NormalizeEmail(request.Email);
        if (await Accounts.AnyAsync(c => c.Email == email, cancellationToken))
            return Result<AuthTokens>.Conflict("Bu e-posta adresiyle kayıtlı bir hesap var.");

        var customer = Customer.Register(email, request.FirstName, request.LastName, request.Phone);
        customer.PasswordHash = HashPassword(customer, request.Password);
        customer.RegisterSuccessfulLogin(Now);
        Accounts.Add(customer);
        await UnitOfWork.SaveChangesAsync(cancellationToken);

        // Kayıt sonrası otomatik giriş; token için müşterinin Id'si gerektiğinden ikinci kayıt yapılır.
        var tokens = IssueTokens(customer, Now);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        return Result<AuthTokens>.Success(tokens);
    }

    public async Task<CustomerProfileDto?> GetProfileAsync(int customerId, CancellationToken cancellationToken = default)
    {
        var customer = await Accounts.GetByIdAsync(customerId, cancellationToken);
        return customer is null ? null : mapper.Map<CustomerProfileDto>(customer);
    }

    public async Task<Result<CustomerProfileDto>> UpdateProfileAsync(int customerId, UpdateCustomerProfileRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await Accounts.GetByIdAsync(customerId, cancellationToken);
        if (customer is null)
            return Result<CustomerProfileDto>.NotFound("Müşteri bulunamadı.");

        customer.UpdateProfile(request.FirstName, request.LastName, request.Phone);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        return Result<CustomerProfileDto>.Success(mapper.Map<CustomerProfileDto>(customer));
    }

    protected override Expression<Func<Customer, bool>> LoginEquals(string login)
    {
        var email = Customer.NormalizeEmail(login);
        return c => c.Email == email;
    }

    protected override Expression<Func<CustomerRefreshToken, bool>> TokenHashEquals(string tokenHash) => t => t.TokenHash == tokenHash;

    protected override Expression<Func<CustomerRefreshToken, bool>> ActiveTokensOf(int accountId) => t => t.CustomerId == accountId && t.RevokedAt == null;
}
