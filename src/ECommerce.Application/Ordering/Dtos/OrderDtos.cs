using ECommerce.Ordering;

namespace ECommerce.Application.Ordering;

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
    string? InvoiceNumber,
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
