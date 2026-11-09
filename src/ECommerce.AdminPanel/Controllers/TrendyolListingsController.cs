using ECommerce.AdminPanel.Api;
using ECommerce.AdminPanel.Models;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.AdminPanel.Controllers;

/// <summary>Trendyol'a gönderilen ürünlerin durumu ve toplu işlem (batch) sonuçları.</summary>
[Route("trendyol")]
public class TrendyolListingsController(ITrendyolIntegrationClient integration, ITrendyolBatchesClient batches) : PanelController
{
    private const int PageSize = 20;

    [HttpGet("listings")]
    public async Task<IActionResult> Index(ListingStatus? status, int page, CancellationToken cancellationToken)
    {
        var result = await integration.ListListingsAsync(status, Math.Max(page, 0), PageSize, cancellationToken);
        return View(new ListingListPage(status, result));
    }

    /// <summary>Arka plan işini beklemeden batch sonuçlarını ve onay durumlarını Trendyol'dan çeker.</summary>
    [HttpPost("listings/refresh")]
    public async Task<IActionResult> Refresh(ListingStatus? status, CancellationToken cancellationToken)
    {
        try
        {
            var result = await integration.SyncListingsAsync(cancellationToken);
            FlashSuccess($"{result.Batches} toplu işlem ve {result.Updated} ürün durumu güncellendi.");
        }
        catch (ApiException exception)
        {
            FlashError(exception);
        }

        return RedirectToAction(nameof(Index), new { status });
    }

    [HttpGet("batches/{batchRequestId}")]
    public async Task<IActionResult> Batch(string batchRequestId, CancellationToken cancellationToken)
    {
        try
        {
            return View(await batches.GetAsync(batchRequestId, cancellationToken));
        }
        catch (ApiException exception) when (exception.StatusCode == StatusCodes.Status404NotFound)
        {
            return NotFound();
        }
    }
}
