using ECommerce.Application.Common;
using ECommerce.Core.Persistence;
using ECommerce.Marketplaces;
using MapsterMapper;
using Trendyol.Client.Products;

namespace ECommerce.Application.Marketplaces.Trendyol;

/// <summary>
/// Bekleyen batch'lerin sonucunu Trendyol'dan sorgular. Trendyol sonuçları ~4 saat sakladığı için daha eski kayıtlar süresi dolmuş sayılır.
/// </summary>
public class TrendyolBatchService(
    ITrendyolProductClient client,
    IRepository<MarketplaceBatchRequest> batches,
    IUnitOfWork unitOfWork,
    TimeProvider clock,
    IMapper mapper) : ITrendyolBatchService
{
    public static readonly TimeSpan ResultRetention = TimeSpan.FromHours(4);

    public async Task<PagedResult<BatchDto>> ListAsync(MarketplaceBatchStatus? status, int page, int size, CancellationToken cancellationToken = default)
    {
        size = Math.Clamp(size, 1, 100);
        page = Math.Max(page, 0);

        var total = await batches.CountAsync(b => b.Marketplace == TrendyolBatchTracker.Marketplace && (status == null || b.Status == status), cancellationToken);
        var items = await batches.ListAsync(q => q
            .Where(b => b.Marketplace == TrendyolBatchTracker.Marketplace && (status == null || b.Status == status))
            .OrderByDescending(b => b.CreatedDate)
            .ThenByDescending(b => b.Id)
            .Skip(page * size)
            .Take(size), cancellationToken);

        return new PagedResult<BatchDto>(page, size, total, items.Select(mapper.Map<BatchDto>).ToList());
    }

    public async Task<BatchDto?> GetAsync(string batchRequestId, CancellationToken cancellationToken = default)
    {
        var batch = await batches.FirstOrDefaultAsync(b => b.Marketplace == TrendyolBatchTracker.Marketplace && b.BatchRequestId == batchRequestId, cancellationToken);
        return batch is null ? null : mapper.Map<BatchDto>(batch);
    }

    public async Task<int> RefreshPendingAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var pending = await batches.ListAsync(
            b => b.Marketplace == TrendyolBatchTracker.Marketplace &&
                 (b.Status == MarketplaceBatchStatus.Pending || b.Status == MarketplaceBatchStatus.InProgress),
            cancellationToken);

        var updated = 0;
        foreach (var batch in pending)
        {
            if (now - batch.CreatedDate > ResultRetention)
            {
                batch.MarkExpired(now);
                updated++;
                continue;
            }

            var response = await client.GetBatchRequestResultAsync(batch.BatchRequestId, cancellationToken);
            if (response.StatusCode == 404)
            {
                batch.MarkExpired(now);
                updated++;
                continue;
            }

            if (!response.IsSuccess)
                continue;

            var result = response.Data!;
            batch.ApplyResult(IsCompleted(result), result.ItemCount, result.FailedItemCount, Failures(result), now);
            updated++;
        }

        if (updated > 0)
            await unitOfWork.SaveChangesAsync(cancellationToken);

        return updated;
    }

    // Fiyat/stok batch'lerinde üst seviye status alanı yoktur; hiçbir kalem işlenmekte değilse tamamlanmış sayılır.
    private static bool IsCompleted(BatchRequestResult result) =>
        result.Status is not null
            ? result.Status == "COMPLETED"
            : result.Items is { Count: > 0 } items && items.All(i => i.Status != "IN_PROGRESS");

    private static IEnumerable<MarketplaceBatchFailure> Failures(BatchRequestResult result) =>
        (result.Items ?? [])
            .Where(i => i.Status == "FAILED")
            .Select(i => new MarketplaceBatchFailure(i.RequestItem?.Barcode, i.FailureReasons ?? []));
}
