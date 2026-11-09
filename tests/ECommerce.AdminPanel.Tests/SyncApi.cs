using System.Net;

namespace ECommerce.AdminPanel.Tests;

/// <summary>Senkron ekranı ve dashboard için API cevapları.</summary>
public static class SyncApi
{
    public const string Base = "/api/v1/marketplaces/sync";

    public static object Summary() => new
    {
        ordersByStatus = new[]
        {
            new { status = "PendingPayment", count = 2 }, new { status = "Confirmed", count = 3 }, new { status = "Preparing", count = 1 },
            new { status = "Shipped", count = 4 }, new { status = "Delivered", count = 10 }, new { status = "Cancelled", count = 0 }
        },
        pendingBankTransfers = 2,
        today = new { from = "2026-11-08T21:00:00Z", total = 1250.5m, orderCount = 3,
            byChannel = new[] { new { channel = "Web", total = 1000.5m, orderCount = 2 }, new { channel = "Trendyol", total = 250m, orderCount = 1 } } },
        last7Days = new { from = "2026-11-02T21:00:00Z", total = 9999.9m, orderCount = 20, byChannel = new[] { new { channel = "Web", total = 9999.9m, orderCount = 20 } } },
        lowStock = new { threshold = 5, totalCount = 21, variants = new[] { new { productId = 7, productName = "Basic Tişört", variantId = 70, sku = "BT-M", barcode = "1", stockQuantity = 0 } } },
        failedOutbox = new { inventory = 3, orderStatus = 0 },
        syncAlerts = new[] { new { marketplace = "Trendyol", job = "PriceInventory", cursor = (string?)null, pausedUntil = (string?)null,
            lastRunAt = "2026-11-09T06:00:00Z", lastError = "Too many requests" } }
    };

    public static FakeApi Register(FakeApi api) => api
        .On("GET", "/api/v1/dashboard/summary", _ => FakeApi.Json(HttpStatusCode.OK, Summary()))
        .On("GET", $"{Base}/states", _ => FakeApi.Json(HttpStatusCode.OK, new[]
        {
            new { marketplace = "Trendyol", job = "OrderImport", cursor = "2026-11-09T05:00:00Z", pausedUntil = "2099-01-01T00:00:00Z",
                lastRunAt = "2026-11-09T06:00:00Z", lastError = (string?)"Too many requests" }
        }))
        .On("GET", $"{Base}/logs", _ => FakeApi.Json(HttpStatusCode.OK, new
        {
            page = 0, size = 50, totalElements = 1, totalPages = 1,
            content = new[] { new { id = 1L, marketplace = "Trendyol", operation = "PriceInventory", reference = "variant:70", succeeded = false,
                message = "Barkod bulunamadı", occurredAt = "2026-11-09T06:00:00Z" } }
        }))
        .On("GET", $"{Base}/outbox/Inventory", _ => FakeApi.Json(HttpStatusCode.OK, new
        {
            page = 0, size = 50, totalElements = 2, totalPages = 1,
            content = new[]
            {
                Entry(5, "Failed", "Geçersiz barkod"),
                Entry(6, "Pending", null)
            }
        }))
        .On("GET", $"{Base}/outbox/OrderStatus", _ => FakeApi.Json(HttpStatusCode.OK, new { page = 0, size = 50, totalElements = 0, totalPages = 0, content = Array.Empty<object>() }));

    private static object Entry(int id, string status, string? error) => new
    {
        id, kind = "Inventory", reference = $"variant:{id}", action = (string?)null, status, attempts = status == "Failed" ? 5 : 0,
        nextAttemptAt = (string?)null, processedAt = (string?)null, error, createdDate = "2026-11-09T06:00:00Z"
    };
}
