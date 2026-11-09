using Asp.Versioning;
using ECommerce.API.Infrastructure;
using ECommerce.Application.Marketplaces.Trendyol;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Trendyol;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/marketplaces/trendyol")]
[Tags("Trendyol - Katalog")]
public class TrendyolCatalogController(ITrendyolCatalogService catalog) : ControllerBase
{
    [HttpGet("categories")]
    [ProducesResponseType<IReadOnlyList<CategoryNodeDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategoryTree(CancellationToken cancellationToken) =>
        this.ToActionResult(await catalog.GetCategoryTreeAsync(cancellationToken), Ok);

    [HttpGet("categories/search")]
    [ProducesResponseType<IReadOnlyList<CategorySearchResultDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchCategories([FromQuery] string term, [FromQuery] int limit = 50, CancellationToken cancellationToken = default) =>
        this.ToActionResult(await catalog.SearchCategoriesAsync(term, Math.Clamp(limit, 1, 200), cancellationToken), Ok);

    [HttpGet("categories/{categoryId:long}/attributes")]
    [ProducesResponseType<IReadOnlyList<CategoryAttributeDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategoryAttributes(long categoryId, CancellationToken cancellationToken) =>
        this.ToActionResult(await catalog.GetCategoryAttributesAsync(categoryId, cancellationToken), Ok);

    [HttpGet("categories/{categoryId:long}/attributes/{attributeId:long}/values")]
    [ProducesResponseType<IReadOnlyList<AttributeValueDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAttributeValues(long categoryId, long attributeId, CancellationToken cancellationToken) =>
        this.ToActionResult(await catalog.GetAttributeValuesAsync(categoryId, attributeId, cancellationToken), Ok);

    [HttpGet("brands")]
    [ProducesResponseType<IReadOnlyList<TrendyolBrandDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchBrands([FromQuery] string name, CancellationToken cancellationToken) =>
        this.ToActionResult(await catalog.SearchBrandsAsync(name, cancellationToken), Ok);
}
