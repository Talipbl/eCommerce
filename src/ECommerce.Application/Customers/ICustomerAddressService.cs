using ECommerce.Application.Common;

namespace ECommerce.Application.Customers;

public interface ICustomerAddressService
{
    Task<IReadOnlyList<AddressDto>> ListAsync(int customerId, CancellationToken cancellationToken = default);
    Task<Result<AddressDto>> AddAsync(int customerId, SaveAddressRequest request, CancellationToken cancellationToken = default);
    Task<Result<AddressDto>> UpdateAsync(int customerId, int addressId, SaveAddressRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(int customerId, int addressId, CancellationToken cancellationToken = default);
    Task<Result<bool>> SetDefaultAsync(int customerId, int addressId, CancellationToken cancellationToken = default);
}
