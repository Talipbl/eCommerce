using ECommerce.AdminPanel.Api;
using ECommerce.AdminPanel.Infrastructure;

namespace ECommerce.AdminPanel.Models;

public sealed record ProductEditPage(
    ProductDetailDto Product,
    ProductDetailsForm Details,
    VariantForm NewVariant,
    IReadOnlyList<BrandDto> Brands,
    IReadOnlyList<CategoryOption> Categories,
    ListingDto? Listing = null)
{
    public const int MaxImages = 8;

    public IReadOnlyList<ProductImageDto> Images => Product.Images.OrderBy(i => i.SortOrder).ToList();
}
