using Asp.Versioning;
using ECommerce.API.Infrastructure;
using ECommerce.Application.Common;
using ECommerce.Application.Marketplaces;
using ECommerce.Marketplaces;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Marketplaces;

/// <summary>Pazaryeri senkron logu, iş durumları ve hatalı kayıtların tekrar denenmesi.</summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/marketplaces/sync")]
[Tags("Pazaryeri - Senkron")]
public class MarketplaceSyncController(IMarketplaceSyncAdminService sync) : ControllerBase
{
    [HttpGet("logs")]
    public async Task<PagedResult<SyncLogDto>> ListLogs(
        [FromQuery] string? marketplace, [FromQuery] SyncOperation? operation, [FromQuery] bool? succeeded,
        [FromQuery] int page = 0, [FromQuery] int size = 50, CancellationToken cancellationToken = default) =>
        await sync.ListLogsAsync(new SyncLogQuery(marketplace, operation, succeeded, page, size), cancellationToken);

    [HttpGet("states")]
    public async Task<IReadOnlyList<SyncStateDto>> ListStates(CancellationToken cancellationToken) =>
        await sync.ListStatesAsync(cancellationToken);

    /// <summary>Outbox kayıtları; kind = Inventory (stok/fiyat) veya OrderStatus (sipariş durum bildirimi).</summary>
    [HttpGet("outbox/{kind}")]
    public async Task<PagedResult<OutboxEntryDto>> ListOutbox(
        OutboxKind kind, [FromQuery] OutboxStatus? status, [FromQuery] int page = 0, [FromQuery] int size = 50, CancellationToken cancellationToken = default) =>
        await sync.ListOutboxAsync(kind, status, page, size, cancellationToken);

    [HttpPost("outbox/{kind}/{id:int}/retry")]
    public async Task<IActionResult> Retry(OutboxKind kind, int id, CancellationToken cancellationToken) =>
        this.ToActionResult(await sync.RetryAsync(kind, id, cancellationToken), Ok);

    [HttpPost("outbox/{kind}/retry-failed")]
    public async Task<IActionResult> RetryFailed(OutboxKind kind, CancellationToken cancellationToken) =>
        Ok(new { requeued = await sync.RetryAllFailedAsync(kind, cancellationToken) });
}
