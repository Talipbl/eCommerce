using ECommerce.Application.Common;
using ECommerce.Marketplaces;

namespace ECommerce.Application.Marketplaces;

public interface IMarketplaceSyncAdminService
{
    Task<PagedResult<SyncLogDto>> ListLogsAsync(SyncLogQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SyncStateDto>> ListStatesAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<OutboxEntryDto>> ListOutboxAsync(OutboxKind kind, OutboxStatus? status, int page, int size, CancellationToken cancellationToken = default);
    Task<Result<OutboxEntryDto>> RetryAsync(OutboxKind kind, int id, CancellationToken cancellationToken = default);
    Task<int> RetryAllFailedAsync(OutboxKind kind, CancellationToken cancellationToken = default);
}
