using Asp.Versioning;
using ECommerce.API.Infrastructure;
using ECommerce.Application.Marketplaces.Trendyol;
using Microsoft.AspNetCore.Mvc;
using Trendyol.Client.Products;

namespace ECommerce.API.Controllers.Marketplaces.Trendyol;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/marketplaces/trendyol/products")]
[Tags("Trendyol - Ürünler")]
public class TrendyolProductsController(ITrendyolProductService products) : ControllerBase
{
    [HttpGet("approved")]
    [ProducesResponseType<ApprovedProductPage>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApproved([FromQuery] ProductListFilter filter, [FromQuery] long? contentId, CancellationToken cancellationToken) =>
        this.ToActionResult(await products.GetApprovedAsync(new ApprovedProductQuery
        {
            Barcode = filter.Barcode,
            ContentId = contentId,
            StockCode = filter.StockCode,
            ProductMainId = filter.ProductMainId,
            BrandIds = filter.BrandIds,
            Status = filter.Status,
            StartDate = filter.StartDate,
            EndDate = filter.EndDate,
            DateQueryType = filter.DateQueryType,
            Page = filter.Page,
            Size = filter.Size,
            NextPageToken = filter.NextPageToken
        }, cancellationToken), Ok);

    [HttpGet("unapproved")]
    [ProducesResponseType<UnapprovedProductPage>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnapproved([FromQuery] ProductListFilter filter, CancellationToken cancellationToken) =>
        this.ToActionResult(await products.GetUnapprovedAsync(new UnapprovedProductQuery
        {
            Barcode = filter.Barcode,
            StockCode = filter.StockCode,
            ProductMainId = filter.ProductMainId,
            BrandIds = filter.BrandIds,
            Status = filter.Status,
            StartDate = filter.StartDate,
            EndDate = filter.EndDate,
            DateQueryType = filter.DateQueryType,
            Page = filter.Page,
            Size = filter.Size,
            NextPageToken = filter.NextPageToken
        }, cancellationToken), Ok);

    [HttpPost]
    [ProducesResponseType<BatchAcceptedDto>(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Create(CreateTrendyolProductsRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await products.CreateAsync(request, cancellationToken), Accepted);

    [HttpPost("content-updates")]
    [ProducesResponseType<BatchAcceptedDto>(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> UpdateContent(UpdateTrendyolContentRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await products.UpdateContentAsync(request, cancellationToken), Accepted);

    [HttpPost("unapproved-updates")]
    [ProducesResponseType<BatchAcceptedDto>(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> UpdateUnapproved(UpdateUnapprovedTrendyolProductsRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await products.UpdateUnapprovedAsync(request, cancellationToken), Accepted);

    [HttpPost("price-inventory")]
    [ProducesResponseType<BatchAcceptedDto>(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> UpdatePriceAndInventory(UpdatePriceInventoryRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await products.UpdatePriceAndInventoryAsync(request, cancellationToken), Accepted);

    [HttpPost("deletions")]
    [ProducesResponseType<BatchAcceptedDto>(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Delete(DeleteTrendyolProductsRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await products.DeleteAsync(request, cancellationToken), Accepted);

    private IActionResult Accepted(BatchAcceptedDto batch) =>
        AcceptedAtAction(nameof(TrendyolBatchesController.Get), "TrendyolBatches", new { batchRequestId = batch.BatchRequestId, version = "1" }, batch);
}
