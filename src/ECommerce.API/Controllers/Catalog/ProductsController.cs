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
    public async Task<ActionResult<ProductDetailDto>> Get(int id, CancellationToken cancellationToken) =>
        this.ToOk(await products.GetAsync(id, cancellationToken));

    [HttpPost]
    [ProducesResponseType<ProductDetailDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateProductRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await products.CreateAsync(request, cancellationToken),
            p => CreatedAtAction(nameof(Get), new { id = p.Id, version = "1" }, p));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductDetailDto>> Update(int id, UpdateProductRequest request, CancellationToken cancellationToken) =>
        this.ToOk(await products.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        this.ToActionResult(await products.DeleteAsync(id, cancellationToken), _ => NoContent());

    [HttpPost("{id:int}/publish")]
    public async Task<ActionResult<ProductDetailDto>> Publish(int id, CancellationToken cancellationToken) =>
        this.ToOk(await products.PublishAsync(id, cancellationToken));

    [HttpPost("{id:int}/unpublish")]
    public async Task<ActionResult<ProductDetailDto>> Unpublish(int id, CancellationToken cancellationToken) =>
        this.ToOk(await products.UnpublishAsync(id, cancellationToken));

    [HttpPost("{id:int}/variants")]
    public async Task<ActionResult<ProductDetailDto>> AddVariant(int id, VariantInput request, CancellationToken cancellationToken) =>
        this.ToOk(await products.AddVariantAsync(id, request, cancellationToken));

    [HttpPut("{id:int}/variants/{variantId:int}")]
    public async Task<ActionResult<ProductDetailDto>> UpdateVariant(int id, int variantId, UpdateVariantRequest request, CancellationToken cancellationToken) =>
        this.ToOk(await products.UpdateVariantAsync(id, variantId, request, cancellationToken));

    [HttpDelete("{id:int}/variants/{variantId:int}")]
    public async Task<ActionResult<ProductDetailDto>> RemoveVariant(int id, int variantId, CancellationToken cancellationToken) =>
        this.ToOk(await products.RemoveVariantAsync(id, variantId, cancellationToken));

    [HttpPost("{id:int}/images")]
    [RequestSizeLimit(ProductImageService.MaxFileSize + 64 * 1024)]
    public async Task<ActionResult<ProductDetailDto>> UploadImage(int id, IFormFile file, [FromForm] string? altText, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        return this.ToOk(await images.UploadAsync(id, stream, file.ContentType, file.Length, altText, cancellationToken));
    }

    [HttpDelete("{id:int}/images/{imageId:int}")]
    public async Task<ActionResult<ProductDetailDto>> DeleteImage(int id, int imageId, CancellationToken cancellationToken) =>
        this.ToOk(await images.DeleteAsync(id, imageId, cancellationToken));

    [HttpPut("{id:int}/images/order")]
    public async Task<ActionResult<ProductDetailDto>> ReorderImages(int id, ReorderImagesRequest request, CancellationToken cancellationToken) =>
        this.ToOk(await images.ReorderAsync(id, request.ImageIds, cancellationToken));
}
