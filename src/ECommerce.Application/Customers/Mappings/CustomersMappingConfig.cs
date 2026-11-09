using ECommerce.Customers;
using Mapster;

namespace ECommerce.Application.Customers;

public class CustomersMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Customer, CustomerProfileDto>();
        config.NewConfig<CustomerAddress, AddressDto>();
    }
}
