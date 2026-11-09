using ECommerce.AdminPanel.Api;
using ECommerce.AdminPanel.Models;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.AdminPanel.Controllers;

/// <summary>Pazaryeri senkron işlerinin durumu, logu, outbox kayıtları ve elle tetikleme.</summary>
[Route("sync")]
public class SyncController(IMarketplaceSyncClient sync, ITrendyolIntegrationClient trendyol) : PanelController
{
    private const int PageSize = 50;

    public const string ListingsJob = "listings";
    public const string InventoryJob = "inventory";
    public const string OrdersJob = "orders";
    public const string OrderStatusJob = "order-status";

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View((await sync.ListStatesAsync(cancellationToken)).ToList());

    [HttpGet("logs")]
    public async Task<IActionResult> Logs(SyncOperation? operation, bool? succeeded, int page, CancellationToken cancellationToken)
    {
        var result = await sync.ListLogsAsync(null, operation, succeeded, Math.Max(page, 0), PageSize, cancellationToken);
        return View(new SyncLogPage(operation, succeeded, result));
    }

    [HttpGet("outbox/{kind}")]
    public async Task<IActionResult> Outbox(OutboxKind kind, OutboxStatus? status, int page, CancellationToken cancellationToken)
    {
        var result = await sync.ListOutboxAsync(kind, status, Math.Max(page, 0), PageSize, cancellationToken);
        return View(new OutboxPage(kind, status, result));
    }

    [HttpPost("outbox/{kind}/{id:int}/retry")]
    public async Task<IActionResult> Retry(OutboxKind kind, int id, OutboxStatus? status, CancellationToken cancellationToken)
    {
        try
        {
            await sync.RetryAsync(kind, id, cancellationToken);
            FlashSuccess("Kayıt yeniden kuyruğa alındı; bir sonraki çalışmada gönderilir.");
        }
        catch (ApiException exception)
        {
            FlashError(exception);
        }

        return RedirectToAction(nameof(Outbox), new { kind, status });
    }

    [HttpPost("outbox/{kind}/retry-failed")]
    public async Task<IActionResult> RetryFailed(OutboxKind kind, CancellationToken cancellationToken)
    {
        try
        {
            var result = await sync.RetryFailedAsync(kind, cancellationToken);
            FlashSuccess($"{result.Requeued} hatalı kayıt yeniden kuyruğa alındı.");
        }
        catch (ApiException exception)
        {
            FlashError(exception);
        }

        return RedirectToAction(nameof(Outbox), new { kind, status = OutboxStatus.Failed });
    }

    /// <summary>Arka plan işini beklemeden çalıştırır. Pazaryeri limiti aşılırsa API 503 ve bekleme süresi döner; bildirimde gösterilir.</summary>
    [HttpPost("run/{job}")]
    public async Task<IActionResult> Run(string job, CancellationToken cancellationToken)
    {
        try
        {
            switch (job)
            {
                case ListingsJob:
                    var listings = await trendyol.SyncListingsAsync(cancellationToken);
                    FlashSuccess($"Ürün durumları: {listings.Batches} toplu işlem ve {listings.Updated} ürün güncellendi.");
                    break;
                case InventoryJob:
                    FlashSuccess($"Fiyat ve stok: {(await trendyol.SyncInventoryAsync(cancellationToken)).Processed} kayıt işlendi.");
                    break;
                case OrdersJob:
                    var orders = await trendyol.SyncOrdersAsync(cancellationToken);
                    if (orders.Error is null)
                        FlashSuccess($"Siparişler: {orders.Imported} yeni, {orders.Updated} güncellendi, {orders.Skipped} atlandı.");
                    else
                        FlashError($"Sipariş içe alma tamamlanamadı: {orders.Error}");
                    break;
                case OrderStatusJob:
                    FlashSuccess($"Sipariş durumu: {(await trendyol.SyncOrderStatusAsync(cancellationToken)).Processed} kayıt işlendi.");
                    break;
                default:
                    return NotFound();
            }
        }
        catch (ApiException exception)
        {
            FlashError(exception);
        }

        return RedirectToAction(nameof(Index));
    }
}
