using Asp.Versioning;
using ECommerce.API.Infrastructure;
using ECommerce.Application.Catalog;
using ECommerce.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Catalog;

/// <summary>Yerel e-ticaret sitesinin herkese açık katalog uçları.</summary>
[ApiController]
[AllowAnonymous]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/store")]
[Tags("Vitrin")]
public class StoreController(IStorefrontService storefront) : ControllerBase
{
    [HttpGet("categories")]
    public async Task<IReadOnlyList<StoreCategoryDto>> GetCategories(CancellationToken cancellationToken) =>
        await storefront.GetCategoriesAsync(cancellationToken);

    [HttpGet("products")]
    public async Task<PagedResult<StoreProductCard>> SearchProducts(
        [FromQuery] string? category, [FromQuery] string? brand, [FromQuery] string? search,
        [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice, [FromQuery] bool inStock = false,
        [FromQuery] StoreProductSort sort = StoreProductSort.Newest, [FromQuery] int page = 0, [FromQuery] int size = 24,
        CancellationToken cancellationToken = default) =>
        await storefront.SearchProductsAsync(new StoreProductQuery
        {
            CategorySlug = category,
            BrandSlug = brand,
            Search = search,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            InStockOnly = inStock,
            Sort = sort,
            Page = page,
            Size = size
        }, cancellationToken);

    [HttpGet("products/{slug}")]
    public async Task<IActionResult> GetProduct(string slug, CancellationToken cancellationToken) =>
        this.ToActionResult(await storefront.GetProductAsync(slug, cancellationToken), Ok);
}
