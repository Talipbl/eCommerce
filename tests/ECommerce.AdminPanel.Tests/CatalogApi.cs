using System.Net;

namespace ECommerce.AdminPanel.Tests;

/// <summary>Katalog ekranlarının ihtiyaç duyduğu API cevapları; testler gerektiğinde rotaları ezer.</summary>
public static class CatalogApi
{
    public const int ProductId = 7;

    public static object Brand(int id, string name, bool isActive = true) => new { id, name, slug = name.ToLowerInvariant(), isActive };

    public static object Node(int id, string name, int? parentId, params object[] children) =>
        new { id, name, slug = name.ToLowerInvariant(), parentId, sortOrder = 0, isActive = true, children };

    public static object Product(params object[] images) => new
    {
        id = ProductId,
        name = "Basic Tişört",
        slug = "basic-tisort",
        productCode = "BT-1",
        brandId = 1,
        categoryId = 11,
        vatRate = 20,
        description = (string?)null,
        isActive = false,
        variants = new[]
        {
            new { id = 70, sku = "BT-M", barcode = "869000000001", price = 1499.9m, listPrice = (decimal?)1999m, stockQuantity = 3, isActive = true,
                  attributes = new[] { new { name = "Beden", value = "M" } } }
        },
        images
    };

    public static object Image(int id, int sortOrder) => new { id, url = $"https://shop.test/media/{id}.jpg", altText = $"görsel {id}", sortOrder };

    public static FakeApi Create() => FakeApi.Admin()
        .On("GET", "/api/v1/catalog/brands", _ => FakeApi.Json(HttpStatusCode.OK, new[] { Brand(2, "Zeytin"), Brand(3, "Çiçek"), Brand(1, "Acme") }))
        .On("GET", "/api/v1/catalog/categories", _ => FakeApi.Json(HttpStatusCode.OK, new[]
        {
            Node(10, "Giyim", null, Node(11, "Tişört", 10, Node(12, "Basic", 11))),
            Node(20, "Ayakkabı", null)
        }))
        .On("GET", "/api/v1/catalog/products", _ => FakeApi.Json(HttpStatusCode.OK, new
        {
            page = 0, size = 20, totalElements = 1, totalPages = 1,
            content = new[] { new { id = ProductId, name = "Basic Tişört", slug = "basic-tisort", productCode = "BT-1", brandId = 1, categoryId = 11,
                isActive = true, variantCount = 1, totalStock = 1250, minPrice = (decimal?)1499.9m, imageUrl = (string?)null } }
        }))
        .On("GET", $"/api/v1/catalog/products/{ProductId}", _ => FakeApi.Json(HttpStatusCode.OK, Product(Image(1, 0), Image(2, 1), Image(3, 2))));
}
