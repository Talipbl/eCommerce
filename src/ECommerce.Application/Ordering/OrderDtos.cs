using ECommerce.Ordering;
using System.Security.Cryptography;

namespace ECommerce.Application.Ordering;

public class CheckoutOptions
{
    public const string SectionName = "Checkout";

    public decimal ShippingFee { get; set; } = 49.90m;
    public decimal FreeShippingThreshold { get; set; } = 750m;
    public decimal CashOnDeliveryFee { get; set; } = 19.90m;
    public BankTransferOptions BankTransfer { get; set; } = new();

    public decimal ShippingFeeFor(decimal subtotal) => subtotal >= FreeShippingThreshold ? 0m : ShippingFee;

    public decimal PaymentFeeFor(PaymentMethod method) => method == PaymentMethod.CashOnDelivery ? CashOnDeliveryFee : 0m;
}

public class BankTransferOptions
{
    public string BankName { get; set; } = string.Empty;
    public string AccountHolder { get; set; } = string.Empty;
    public string Iban { get; set; } = string.Empty;
}

public record CheckoutRequest(int AddressId, PaymentMethod PaymentMethod, string? Note);

public record CheckoutPreviewDto(decimal Subtotal, decimal ShippingFee, decimal PaymentFee, decimal Total);

/// <summary>Pazaryeri siparişinde kargo bilgisi boş bırakılabilir (pazaryerinden gelir), fatura numarası zorunludur.</summary>
public record ShipOrderRequest(string? CargoProvider, string? TrackingNumber, string? InvoiceNumber = null);

public record CancelOrderRequest(string Reason);

public record OrderAdminQuery(OrderStatus? Status, OrderChannel? Channel, string? Search, int Page = 0, int Size = 20);

public record OrderAddressDto(string FullName, string Phone, string City, string District, string AddressLine, string? PostalCode);

public record OrderLineDto(int Id, int? VariantId, int? ProductId, string ProductName, string? VariantDescription, string Barcode, decimal UnitPrice, int Quantity, int VatRate, decimal LineTotal);

public record OrderStatusChangeDto(OrderStatus Status, DateTime ChangedAt, string? Note);

public record BankTransferDto(string BankName, string AccountHolder, string Iban, string Description);

public record OrderDto(
    int Id,
    string OrderNumber,
    OrderChannel Channel,
    string? ExternalReference,
    OrderStatus Status,
    PaymentMethod PaymentMethod,
    string CustomerEmail,
    string CustomerName,
    OrderAddressDto ShippingAddress,
    IReadOnlyList<OrderLineDto> Lines,
    decimal Subtotal,
    decimal ShippingFee,
    decimal PaymentFee,
    decimal Total,
    string? Note,
    string? CargoProvider,
    string? TrackingNumber,
    string? CancelReason,
    DateTime PlacedAt,
    DateTime? PaidAt,
    DateTime? ShippedAt,
    DateTime? DeliveredAt,
    DateTime? CancelledAt,
    bool CanCustomerCancel,
    IReadOnlyList<OrderStatusChangeDto> History,
    BankTransferDto? BankTransfer);

public record OrderSummaryDto(int Id, string OrderNumber, OrderChannel Channel, OrderStatus Status, PaymentMethod PaymentMethod, string CustomerName, decimal Total, int ItemCount, DateTime PlacedAt);

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

public static class OrderNumbers
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    /// <summary>Okunması kolay, tahmin edilmesi zor sipariş numarası (ör. EC261107K7M2QX).</summary>
    public static string New(DateTime now) =>
        "EC" + now.ToString("yyMMdd") + RandomNumberGenerator.GetString(Alphabet, 6);
}
