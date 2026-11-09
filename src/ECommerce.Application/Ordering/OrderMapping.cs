using ECommerce.Ordering;
using MapsterMapper;

namespace ECommerce.Application.Ordering;

internal static class OrderMapping
{
    public static OrderDto ToOrderDto(this IMapper mapper, Order order, CheckoutOptions options) =>
        mapper.Map<OrderDto>(order) with { BankTransfer = BankTransfer(order, options) };

    private static BankTransferDto? BankTransfer(Order o, CheckoutOptions options)
    {
        if (o.Status != OrderStatus.PendingPayment || o.PaymentMethod != PaymentMethod.BankTransfer)
            return null;

        var bank = options.BankTransfer;
        return new BankTransferDto(bank.BankName, bank.AccountHolder, bank.Iban, $"Açıklamaya sipariş numarasını yazın: {o.OrderNumber}");
    }
}
