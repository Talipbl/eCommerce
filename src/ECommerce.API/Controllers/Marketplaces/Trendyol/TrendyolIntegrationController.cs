using Asp.Versioning;
using ECommerce.API.Controllers.Marketplaces;
using ECommerce.API.Infrastructure;
using ECommerce.Application.Common;
using ECommerce.Application.Marketplaces;
using ECommerce.Application.Marketplaces.Trendyol;
using ECommerce.Marketplaces;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Marketplaces.Trendyol;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/marketplaces/trendyol")]
[Tags("Trendyol - Entegrasyon")]
public class TrendyolIntegrationController(
    ITrendyolListingService listings,
    ITrendyolBatchService batches,
    IMarketplaceInventorySyncService inventory,
    IMarketplaceOrderImportService orderImport,
    IMarketplaceOrderStatusSyncService orderStatus) : ControllerBase
{
    [HttpGet("mappings/categories")]
    public async Task<IReadOnlyList<CategoryMappingDto>> ListCategoryMappings(CancellationToken cancellationToken) =>
        await listings.ListCategoryMappingsAsync(cancellationToken);

    [HttpPut("mappings/categories/{categoryId:int}")]
    public async Task<ActionResult<CategoryMappingDto>> MapCategory(int categoryId, MapCategoryRequest request, CancellationToken cancellationToken) =>
        this.ToOk(await listings.MapCategoryAsync(categoryId, request, cancellationToken));

    /// <summary>Kategori özellik eşlemesi: sabit değerler ve varyant özelliği → Trendyol değer eşlemeleri (tümünü değiştirir).</summary>
    [HttpPut("mappings/categories/{categoryId:int}/attributes")]
    public async Task<ActionResult<CategoryMappingDto>> MapCategoryAttributes(int categoryId, SetCategoryAttributesRequest request, CancellationToken cancellationToken) =>
        this.ToOk(await listings.SetCategoryAttributesAsync(categoryId, request, cancellationToken));

    [HttpGet("mappings/brands")]
    public async Task<IReadOnlyList<BrandMappingDto>> ListBrandMappings(CancellationToken cancellationToken) =>
        await listings.ListBrandMappingsAsync(cancellationToken);

    [HttpPut("mappings/brands/{brandId:int}")]
    public async Task<ActionResult<BrandMappingDto>> MapBrand(int brandId, MapBrandRequest request, CancellationToken cancellationToken) =>
        this.ToOk(await listings.MapBrandAsync(brandId, request, cancellationToken));

    [HttpGet("listings")]
    public async Task<PagedResult<ListingSummaryDto>> ListListings(
        [FromQuery] ListingStatus? status, [FromQuery] int page = 0, [FromQuery] int size = 20, CancellationToken cancellationToken = default) =>
        await listings.ListAsync(new ListingQuery(status, page, size), cancellationToken);

    [HttpGet("listings/{productId:int}")]
    public async Task<ActionResult<ListingDto>> GetListing(int productId, CancellationToken cancellationToken) =>
        this.ToOk(await listings.GetAsync(productId, cancellationToken));

    [HttpPost("listings/{productId:int}")]
    [ProducesResponseType<ListingDto>(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Publish(int productId, PublishListingRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await listings.PublishAsync(productId, request, cancellationToken), l => StatusCode(StatusCodes.Status202Accepted, l));

    /// <summary>Arka plan işlerini beklemeden elle tetikler.</summary>
    [HttpPost("sync/listings")]
    public async Task<ListingSyncResponse> SyncListings(CancellationToken cancellationToken) =>
        new(await batches.RefreshPendingAsync(cancellationToken), await listings.RefreshStatusesAsync(cancellationToken));

    [HttpPost("sync/inventory")]
    public async Task<ProcessedCountResponse> SyncInventory(CancellationToken cancellationToken) =>
        new(await inventory.SyncPendingAsync(cancellationToken));

    [HttpPost("sync/orders")]
    public async Task<OrderImportSummary> SyncOrders(CancellationToken cancellationToken) =>
        await orderImport.ImportAsync(MarketplaceNames.Trendyol, cancellationToken);

    [HttpPost("sync/order-status")]
    public async Task<ProcessedCountResponse> SyncOrderStatus(CancellationToken cancellationToken) =>
        new(await orderStatus.SyncPendingAsync(cancellationToken));
}
