using Asp.Versioning;
using ECommerce.API.Infrastructure;
using ECommerce.Application.Catalog;
using ECommerce.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Catalog;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/catalog/products")]
[Tags("Katalog - Ürünler")]
public class ProductsController(IProductAdminService products, IProductImageService images) : ControllerBase
{
    [HttpGet]
    public async Task<PagedResult<ProductListItemDto>> List(
        [FromQuery] string? search, [FromQuery] int? categoryId, [FromQuery] int? brandId, [FromQuery] bool? isActive,
        [FromQuery] int page = 0, [FromQuery] int size = 20, CancellationToken cancellationToken = default) =>
        await products.ListAsync(new ProductAdminQuery(search, categoryId, brandId, isActive, page, size), cancellationToken);

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken) =>
        this.ToActionResult(await products.GetAsync(id, cancellationToken), Ok);

    [HttpPost]
    [ProducesResponseType<ProductDetailDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateProductRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await products.CreateAsync(request, cancellationToken),
            p => CreatedAtAction(nameof(Get), new { id = p.Id, version = "1" }, p));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateProductRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await products.UpdateAsync(id, request, cancellationToken), Ok);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        this.ToActionResult(await products.DeleteAsync(id, cancellationToken), _ => NoContent());

    [HttpPost("{id:int}/publish")]
    public async Task<IActionResult> Publish(int id, CancellationToken cancellationToken) =>
        this.ToActionResult(await products.PublishAsync(id, cancellationToken), Ok);

    [HttpPost("{id:int}/unpublish")]
    public async Task<IActionResult> Unpublish(int id, CancellationToken cancellationToken) =>
        this.ToActionResult(await products.UnpublishAsync(id, cancellationToken), Ok);

    [HttpPost("{id:int}/variants")]
    public async Task<IActionResult> AddVariant(int id, VariantInput request, CancellationToken cancellationToken) =>
        this.ToActionResult(await products.AddVariantAsync(id, request, cancellationToken), Ok);

    [HttpPut("{id:int}/variants/{variantId:int}")]
    public async Task<IActionResult> UpdateVariant(int id, int variantId, UpdateVariantRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await products.UpdateVariantAsync(id, variantId, request, cancellationToken), Ok);

    [HttpDelete("{id:int}/variants/{variantId:int}")]
    public async Task<IActionResult> RemoveVariant(int id, int variantId, CancellationToken cancellationToken) =>
        this.ToActionResult(await products.RemoveVariantAsync(id, variantId, cancellationToken), Ok);

    [HttpPost("{id:int}/images")]
    [RequestSizeLimit(ProductImageService.MaxFileSize + 64 * 1024)]
    public async Task<IActionResult> UploadImage(int id, IFormFile file, [FromForm] string? altText, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        return this.ToActionResult(await images.UploadAsync(id, stream, file.ContentType, file.Length, altText, cancellationToken), Ok);
    }

    [HttpDelete("{id:int}/images/{imageId:int}")]
    public async Task<IActionResult> DeleteImage(int id, int imageId, CancellationToken cancellationToken) =>
        this.ToActionResult(await images.DeleteAsync(id, imageId, cancellationToken), Ok);

    [HttpPut("{id:int}/images/order")]
    public async Task<IActionResult> ReorderImages(int id, ReorderImagesRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await images.ReorderAsync(id, request.ImageIds, cancellationToken), Ok);
}
