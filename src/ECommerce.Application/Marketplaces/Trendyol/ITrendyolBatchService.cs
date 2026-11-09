using ECommerce.Application.Common;
using ECommerce.Marketplaces;

namespace ECommerce.Application.Marketplaces.Trendyol;

public interface ITrendyolBatchService
{
    Task<PagedResult<BatchDto>> ListAsync(MarketplaceBatchStatus? status, int page, int size, CancellationToken cancellationToken = default);
    Task<BatchDto?> GetAsync(string batchRequestId, CancellationToken cancellationToken = default);
    Task<int> RefreshPendingAsync(CancellationToken cancellationToken = default);
}
