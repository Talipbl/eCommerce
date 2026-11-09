using ECommerce.Application.Common;
using ECommerce.Core.Persistence;
using ECommerce.Customers;
using MapsterMapper;

namespace ECommerce.Application.Customers;

public class CustomerAddressService(IRepository<Customer> customers, IUnitOfWork unitOfWork, IMapper mapper)
    : ICustomerAddressService
{
    public async Task<IReadOnlyList<AddressDto>> ListAsync(int customerId, CancellationToken cancellationToken = default)
    {
        var customer = await customers.FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);
        return customer is null ? [] : customer.Addresses.OrderByDescending(a => a.IsDefault).ThenBy(a => a.Id).Select(mapper.Map<AddressDto>).ToList();
    }

    public async Task<Result<AddressDto>> AddAsync(int customerId, SaveAddressRequest r, CancellationToken cancellationToken = default)
    {
        var customer = await customers.FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);
        if (customer is null)
            return Result<AddressDto>.NotFound("Müşteri bulunamadı.");

        var address = customer.AddAddress(r.Title, r.FullName, r.Phone, r.City, r.District, r.AddressLine, r.PostalCode);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<AddressDto>.Success(mapper.Map<AddressDto>(address));
    }

    public Task<Result<AddressDto>> UpdateAsync(int customerId, int addressId, SaveAddressRequest r, CancellationToken cancellationToken = default) =>
        WithAddressAsync(customerId, addressId, (_, a) =>
        {
            a.Update(r.Title, r.FullName, r.Phone, r.City, r.District, r.AddressLine, r.PostalCode);
            return mapper.Map<AddressDto>(a);
        }, cancellationToken);

    public async Task<Result<bool>> DeleteAsync(int customerId, int addressId, CancellationToken cancellationToken = default)
    {
        var result = await WithAddressAsync(customerId, addressId, (c, a) =>
        {
            c.RemoveAddress(a);
            return true;
        }, cancellationToken);
        return result;
    }

    public Task<Result<bool>> SetDefaultAsync(int customerId, int addressId, CancellationToken cancellationToken = default) =>
        WithAddressAsync(customerId, addressId, (c, a) =>
        {
            c.SetDefaultAddress(a);
            return true;
        }, cancellationToken);

    private async Task<Result<T>> WithAddressAsync<T>(int customerId, int addressId, Func<Customer, CustomerAddress, T> change, CancellationToken cancellationToken)
    {
        var customer = await customers.FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);
        var address = customer?.Addresses.FirstOrDefault(a => a.Id == addressId);
        if (customer is null || address is null)
            return Result<T>.NotFound("Adres bulunamadı.");

        var value = change(customer, address);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<T>.Success(value);
    }
}
