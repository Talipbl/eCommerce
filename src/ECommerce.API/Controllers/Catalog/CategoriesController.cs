using Asp.Versioning;
using ECommerce.API.Infrastructure;
using ECommerce.Application.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Catalog;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/catalog/categories")]
[Tags("Katalog - Kategoriler")]
public class CategoriesController(ICategoryAdminService categories) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<CategoryTreeDto>> GetTree(CancellationToken cancellationToken) =>
        await categories.GetTreeAsync(cancellationToken);

    [HttpPost]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(SaveCategoryRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await categories.CreateAsync(request, cancellationToken), c => StatusCode(StatusCodes.Status201Created, c));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, SaveCategoryRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await categories.UpdateAsync(id, request, cancellationToken), Ok);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        this.ToActionResult(await categories.DeleteAsync(id, cancellationToken), _ => NoContent());
}
