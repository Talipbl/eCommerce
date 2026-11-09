using ECommerce.Application.Catalog;

namespace ECommerce.Application.Shopping;

public record AddCartItemRequest(int VariantId, int Quantity);

public record UpdateCartItemRequest(int Quantity);

public record CartItemDto(
    int VariantId,
    int ProductId,
    string ProductName,
    string ProductSlug,
    IReadOnlyList<VariantAttributeDto> Attributes,
    decimal UnitPrice,
    decimal? ListPrice,
    int Quantity,
    decimal LineTotal,
    int AvailableStock,
    bool IsAvailable,
    string? ImageUrl);

public record CartDto(IReadOnlyList<CartItemDto> Items, decimal Subtotal, int ItemCount, bool HasUnavailableItems);
