using System.Net;

namespace ECommerce.AdminPanel.Tests;

/// <summary>
/// Trendyol ekranları için API cevapları. Yerel 11 (Tişört) → Trendyol 411; özellikler: Renk (zorunlu), Beden (varyant, eşli),
/// Materyal (isteğe bağlı, serbest değer), Desen (isteğe bağlı).
/// </summary>
public static class TrendyolApi
{
    public const string Base = "/api/v1/marketplaces/trendyol";

    private static object Attribute(long id, string name, bool required = false, bool allowCustom = false, bool varianter = false) =>
        new { id, name, required, allowCustom, varianter, slicer = false, allowMultipleValues = false };

    private static object[] Values(params (long Id, string Value)[] values) => values.Select(v => (object)new { id = v.Id, value = v.Value }).ToArray();

    public static FakeApi Register(FakeApi api) => api
        .On("GET", $"{Base}/mappings/brands", _ => FakeApi.Json(HttpStatusCode.OK, new[] { new { brandId = 1, externalBrandId = 10L, externalBrandName = "Acme TY" } }))
        .On("GET", $"{Base}/mappings/categories", _ => FakeApi.Json(HttpStatusCode.OK, new[]
        {
            new
            {
                categoryId = 11, externalCategoryId = 411L, externalCategoryName = "Tişört",
                attributes = new[] { new { externalAttributeId = 338L, localAttributeName = "Beden", defaultValueId = (long?)null, defaultCustomValue = (string?)null,
                    values = new[] { new { localValue = "M", valueId = (long?)2002, customValue = (string?)null } } } }
            }
        }))
        .On("GET", $"{Base}/categories/411/attributes", _ => FakeApi.Json(HttpStatusCode.OK, new[]
        {
            Attribute(47, "Renk", required: true), Attribute(338, "Beden", required: true, varianter: true),
            Attribute(14, "Materyal", allowCustom: true), Attribute(99, "Desen")
        }))
        .On("GET", $"{Base}/categories/411/attributes/47/values", _ => FakeApi.Json(HttpStatusCode.OK, Values((1, "Mavi"), (2, "Kırmızı"))))
        .On("GET", $"{Base}/categories/411/attributes/338/values", _ => FakeApi.Json(HttpStatusCode.OK, Values((2001, "S"), (2002, "M"))))
        .On("GET", $"{Base}/categories/411/attributes/14/values", _ => FakeApi.Json(HttpStatusCode.OK, Values()))
        .On("GET", $"{Base}/categories/411/attributes/99/values", _ => FakeApi.Json(HttpStatusCode.OK, Values((5, "Düz"))))
        .On("GET", $"{Base}/brands", _ => FakeApi.Json(HttpStatusCode.OK, new[] { new { id = 10L, name = "Acme TY" }, new { id = 12L, name = "Zeytin Moda" } }))
        .On("GET", $"{Base}/categories/search", _ => FakeApi.Json(HttpStatusCode.OK, new[] { new { id = 411L, name = "Tişört", path = "Giyim > Kadın > Tişört" } }))
        .On("GET", $"{Base}/listings", _ => FakeApi.Json(HttpStatusCode.OK, new
        {
            page = 0, size = 20, totalElements = 1, totalPages = 1,
            content = new[] { new { productId = 7, productName = "Basic Tişört", productCode = "BT-1", status = "Rejected", externalContentId = (long?)null,
                lastBatchRequestId = "batch-1", statusMessage = "Görsel kalitesi yetersiz", lastSubmittedAt = "2026-11-09T06:00:00Z", lastCheckedAt = "2026-11-09T07:00:00Z" } }
        }))
        .On("GET", $"{Base}/batches/batch-1", _ => FakeApi.Json(HttpStatusCode.OK, new
        {
            batchRequestId = "batch-1", operation = "ProductCreate", status = "Completed", itemCount = 2, failedItemCount = 1,
            createdAt = "2026-11-09T06:00:00Z", lastCheckedAt = "2026-11-09T06:05:00Z", completedAt = "2026-11-09T06:05:00Z",
            failures = new[] { new { barcode = "869000000001", reasons = new[] { "Görsel kalitesi yetersiz", "Açıklama çok kısa" } } }
        }));

    public static object Listing(string status) => new
    {
        productId = 7, status, externalContentId = (long?)null, lastBatchRequestId = "batch-1", statusMessage = (string?)null,
        lastSubmittedAt = "2026-11-09T06:00:00Z", lastCheckedAt = (string?)null, productAttributes = Array.Empty<object>(),
        variantAttributes = Array.Empty<object>(), deliveryDuration = (int?)2
    };
}
