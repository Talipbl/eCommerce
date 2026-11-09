using Asp.Versioning;
using ECommerce.API.Infrastructure;
using ECommerce.Application.Marketplaces.Trendyol;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Marketplaces.Trendyol;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/marketplaces/trendyol")]
[Tags("Trendyol - Katalog")]
public class TrendyolCatalogController(ITrendyolCatalogService catalog) : ControllerBase
{
    [HttpGet("categories")]
    [ProducesResponseType<IReadOnlyList<CategoryNodeDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryNodeDto>>> GetCategoryTree(CancellationToken cancellationToken) =>
        this.ToOk(await catalog.GetCategoryTreeAsync(cancellationToken));

    [HttpGet("categories/search")]
    [ProducesResponseType<IReadOnlyList<CategorySearchResultDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategorySearchResultDto>>> SearchCategories([FromQuery] string term, [FromQuery] int limit = 50, CancellationToken cancellationToken = default) =>
        this.ToOk(await catalog.SearchCategoriesAsync(term, Math.Clamp(limit, 1, 200), cancellationToken));

    [HttpGet("categories/{categoryId:long}/attributes")]
    [ProducesResponseType<IReadOnlyList<CategoryAttributeDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryAttributeDto>>> GetCategoryAttributes(long categoryId, CancellationToken cancellationToken) =>
        this.ToOk(await catalog.GetCategoryAttributesAsync(categoryId, cancellationToken));

    [HttpGet("categories/{categoryId:long}/attributes/{attributeId:long}/values")]
    [ProducesResponseType<IReadOnlyList<AttributeValueDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AttributeValueDto>>> GetAttributeValues(long categoryId, long attributeId, CancellationToken cancellationToken) =>
        this.ToOk(await catalog.GetAttributeValuesAsync(categoryId, attributeId, cancellationToken));

    [HttpGet("brands")]
    [ProducesResponseType<IReadOnlyList<TrendyolBrandDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TrendyolBrandDto>>> SearchBrands([FromQuery] string name, CancellationToken cancellationToken) =>
        this.ToOk(await catalog.SearchBrandsAsync(name, cancellationToken));
}
