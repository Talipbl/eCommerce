using ECommerce.Ordering;

namespace ECommerce.Application.Ordering;

internal static class OrderMapping
{
    public static OrderDto ToDto(this Order o, CheckoutOptions options)
    {
        var a = o.ShippingAddress;
        var bank = options.BankTransfer;
        return new OrderDto(
            o.Id, o.OrderNumber, o.Channel, o.ExternalReference, o.Status, o.PaymentMethod, o.CustomerEmail, o.CustomerName,
            new OrderAddressDto(a.FullName, a.Phone, a.City, a.District, a.AddressLine, a.PostalCode),
            o.Lines.Select(l => new OrderLineDto(l.Id, l.VariantId, l.ProductId, l.ProductName, l.VariantDescription, l.Barcode, l.UnitPrice, l.Quantity, l.VatRate, l.LineTotal)).ToList(),
            o.Subtotal, o.ShippingFee, o.PaymentFee, o.Total, o.Note, o.CargoProvider, o.TrackingNumber, o.CancelReason,
            o.PlacedAt, o.PaidAt, o.ShippedAt, o.DeliveredAt, o.CancelledAt, o.CanCustomerCancel,
            o.History.OrderBy(h => h.ChangedAt).ThenBy(h => h.Id).Select(h => new OrderStatusChangeDto(h.Status, h.ChangedAt, h.Note)).ToList(),
            o.Status == OrderStatus.PendingPayment && o.PaymentMethod == PaymentMethod.BankTransfer
                ? new BankTransferDto(bank.BankName, bank.AccountHolder, bank.Iban, $"Açıklamaya sipariş numarasını yazın: {o.OrderNumber}")
                : null);
    }

    public static OrderSummaryDto ToSummary(this Order o) =>
        new(o.Id, o.OrderNumber, o.Channel, o.Status, o.PaymentMethod, o.CustomerName, o.Total, o.Lines.Sum(l => l.Quantity), o.PlacedAt);
}
