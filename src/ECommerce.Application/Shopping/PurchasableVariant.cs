using ECommerce.Catalog;

namespace ECommerce.Application.Shopping;

/// <summary>Bir varyantın o anki satılabilirlik bilgisi; sepet ve sipariş aynı kuralları kullanır.</summary>
public sealed record PurchasableVariant(Product Product, ProductVariant Variant)
{
    public bool IsPurchasable => Product.IsActive && Variant.IsActive;
}
