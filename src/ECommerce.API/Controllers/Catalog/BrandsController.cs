using Asp.Versioning;
using ECommerce.API.Infrastructure;
using ECommerce.Application.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Catalog;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/catalog/brands")]
[Tags("Katalog - Markalar")]
public class BrandsController(IBrandAdminService brands) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<BrandDto>> List([FromQuery] string? search, CancellationToken cancellationToken) =>
        await brands.ListAsync(search, cancellationToken);

    [HttpPost]
    [ProducesResponseType<BrandDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(SaveBrandRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await brands.CreateAsync(request, cancellationToken), b => StatusCode(StatusCodes.Status201Created, b));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<BrandDto>> Update(int id, SaveBrandRequest request, CancellationToken cancellationToken) =>
        this.ToOk(await brands.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        this.ToActionResult(await brands.DeleteAsync(id, cancellationToken), _ => NoContent());
}
