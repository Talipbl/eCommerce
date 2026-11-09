using ECommerce.Ordering;
using Mapster;

namespace ECommerce.Application.Ordering;

public class OrderingMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<OrderAddress, OrderAddressDto>();
        config.NewConfig<OrderLine, OrderLineDto>();
        config.NewConfig<OrderStatusChange, OrderStatusChangeDto>();

        // Banka havalesi bilgisi ayarlardan gelir; OrderMapping.ToOrderDto doldurur.
        config.NewConfig<Order, OrderDto>()
            .Map(d => d.History, s => s.History.OrderBy(h => h.ChangedAt).ThenBy(h => h.Id))
            .Ignore(d => d.BankTransfer!);

        config.NewConfig<Order, OrderSummaryDto>()
            .Map(d => d.ItemCount, s => s.Lines.Sum(l => l.Quantity));
    }
}
