using System.Net;

namespace ECommerce.AdminPanel.Tests;

/// <summary>Sipariş ekranları için API cevapları: 1 = havale bekleyen site siparişi, 2 = hazırlanan Trendyol siparişi.</summary>
public static class OrderApi
{
    public const int WebOrderId = 1;
    public const int TrendyolOrderId = 2;

    public static object Order(int id, string channel, string status, string paymentMethod, string? cargoProvider = null, string? trackingNumber = null) => new
    {
        id,
        orderNumber = $"EC{id:000}",
        channel,
        externalReference = channel == "Trendyol" ? "3330000001" : null,
        status,
        paymentMethod,
        customerEmail = "ali@example.com",
        customerName = "Ali Veli",
        shippingAddress = new { fullName = "Ali Veli", phone = "05550000000", city = "İstanbul", district = "Kadıköy", addressLine = "Moda Cd. 1", postalCode = (string?)null },
        lines = new[] { new { id = 1, variantId = 70, productId = 7, productName = "Basic Tişört", variantDescription = "Beden: M", barcode = "869000000001",
            unitPrice = 1499.9m, quantity = 2, vatRate = 20, lineTotal = 2999.8m } },
        subtotal = 2999.8m, shippingFee = 0m, paymentFee = 0m, total = 2999.8m,
        note = (string?)null, cargoProvider, trackingNumber, invoiceNumber = (string?)null, cancelReason = (string?)null,
        placedAt = "2026-11-09T06:00:00Z", paidAt = (string?)null, shippedAt = (string?)null, deliveredAt = (string?)null, cancelledAt = (string?)null,
        canCustomerCancel = false,
        history = new[] { new { status, changedAt = "2026-11-09T06:00:00Z", note = (string?)null } },
        bankTransfer = paymentMethod == "BankTransfer" ? new { bankName = "Banka", accountHolder = "Mağaza", iban = "TR00", description = "EC001" } : null
    };

    public static FakeApi Register(FakeApi api) => api
        .On("GET", "/api/v1/orders", _ => FakeApi.Json(HttpStatusCode.OK, new
        {
            page = 0, size = 20, totalElements = 1, totalPages = 1,
            content = new[] { new { id = WebOrderId, orderNumber = "EC001", channel = "Trendyol", status = "Shipped", paymentMethod = "Marketplace",
                customerName = "Ali Veli", total = 2999.8m, itemCount = 2, placedAt = "2026-11-09T06:00:00Z" } }
        }))
        .On("GET", $"/api/v1/orders/{WebOrderId}", _ => FakeApi.Json(HttpStatusCode.OK, Order(WebOrderId, "Web", "PendingPayment", "BankTransfer")))
        .On("GET", $"/api/v1/orders/{TrendyolOrderId}", _ => FakeApi.Json(HttpStatusCode.OK,
            Order(TrendyolOrderId, "Trendyol", "Preparing", "Marketplace", "Trendyol Express", "7330000000001")));
}
