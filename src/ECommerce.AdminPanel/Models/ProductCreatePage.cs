using ECommerce.AdminPanel.Api;
using ECommerce.AdminPanel.Infrastructure;

namespace ECommerce.AdminPanel.Models;

public sealed record ProductCreatePage(
    ProductDetailsForm Details,
    VariantForm Variant,
    IReadOnlyList<BrandDto> Brands,
    IReadOnlyList<CategoryOption> Categories);
