using ECommerce.AdminPanel.Api;
using ECommerce.AdminPanel.Infrastructure;

namespace ECommerce.AdminPanel.Models;

public sealed record ProductEditPage(
    ProductDetailDto Product,
    ProductDetailsForm Details,
    VariantForm NewVariant,
    IReadOnlyList<BrandDto> Brands,
    IReadOnlyList<CategoryOption> Categories,
    ListingDto? Listing = null,
    string? ActiveForm = null)
{
    public const int MaxImages = 8;

    /// <summary>Özet hata, gönderilen formun yanında gösterilir; sayfada birden çok form var.</summary>
    public const string DetailsForm = "details";
    public const string VariantForm = "variant";

    public IReadOnlyList<ProductImageDto> Images => Product.Images.OrderBy(i => i.SortOrder).ToList();
}
