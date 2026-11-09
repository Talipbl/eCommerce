using ECommerce.Ordering;

namespace ECommerce.Application.Ordering;

public record CheckoutRequest(int AddressId, PaymentMethod PaymentMethod, string? Note);

public record CheckoutPreviewDto(decimal Subtotal, decimal ShippingFee, decimal PaymentFee, decimal Total);
