using Asp.Versioning;
using ECommerce.API.Infrastructure;
using ECommerce.Application.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Customers;

[ApiController]
[ApiVersion(1.0)]
[Authorize(Policy = SecurityRegistration.CustomerPolicy)]
[Route("api/v{version:apiVersion}/customer")]
[Tags("Müşteri - Hesap")]
public class CustomerAccountController(ICustomerAuthService auth, ICustomerAddressService addresses) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var profile = await auth.GetProfileAsync(User.GetAccountId(), cancellationToken);
        return profile is null ? Problem(title: "Oturum geçersiz.", statusCode: StatusCodes.Status401Unauthorized) : Ok(profile);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile(UpdateCustomerProfileRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await auth.UpdateProfileAsync(User.GetAccountId(), request, cancellationToken), Ok);

    [HttpGet("addresses")]
    public async Task<IReadOnlyList<AddressDto>> ListAddresses(CancellationToken cancellationToken) =>
        await addresses.ListAsync(User.GetAccountId(), cancellationToken);

    [HttpPost("addresses")]
    public async Task<IActionResult> AddAddress(SaveAddressRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await addresses.AddAsync(User.GetAccountId(), request, cancellationToken), a => StatusCode(StatusCodes.Status201Created, a));

    [HttpPut("addresses/{addressId:int}")]
    public async Task<IActionResult> UpdateAddress(int addressId, SaveAddressRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await addresses.UpdateAsync(User.GetAccountId(), addressId, request, cancellationToken), Ok);

    [HttpDelete("addresses/{addressId:int}")]
    public async Task<IActionResult> DeleteAddress(int addressId, CancellationToken cancellationToken) =>
        this.ToActionResult(await addresses.DeleteAsync(User.GetAccountId(), addressId, cancellationToken), _ => NoContent());

    [HttpPost("addresses/{addressId:int}/default")]
    public async Task<IActionResult> SetDefaultAddress(int addressId, CancellationToken cancellationToken) =>
        this.ToActionResult(await addresses.SetDefaultAsync(User.GetAccountId(), addressId, cancellationToken), _ => NoContent());
}
