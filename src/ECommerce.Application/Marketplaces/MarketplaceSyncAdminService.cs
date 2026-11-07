using ECommerce.Application.Common;
using ECommerce.Core.Persistence;
using ECommerce.Marketplaces;

namespace ECommerce.Application.Marketplaces;

public enum OutboxKind
{
    Inventory,
    OrderStatus
}

public record SyncLogQuery(string? Marketplace, SyncOperation? Operation, bool? Succeeded, int Page = 0, int Size = 50);

public record SyncLogDto(long Id, string Marketplace, SyncOperation Operation, string? Reference, bool Succeeded, string? Message, DateTime OccurredAt);

public record SyncStateDto(string Marketplace, string Job, DateTime? Cursor, DateTime? PausedUntil, DateTime? LastRunAt, string? LastError);

public record OutboxEntryDto(
    int Id,
    OutboxKind Kind,
    string Reference,
    string? Action,
    OutboxStatus Status,
    int Attempts,
    DateTime? NextAttemptAt,
    DateTime? ProcessedAt,
    string? Error,
    DateTime CreatedDate);

public interface IMarketplaceSyncAdminService
{
    Task<PagedResult<SyncLogDto>> ListLogsAsync(SyncLogQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SyncStateDto>> ListStatesAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<OutboxEntryDto>> ListOutboxAsync(OutboxKind kind, OutboxStatus? status, int page, int size, CancellationToken cancellationToken = default);
    Task<Result<OutboxEntryDto>> RetryAsync(OutboxKind kind, int id, CancellationToken cancellationToken = default);
    Task<int> RetryAllFailedAsync(OutboxKind kind, CancellationToken cancellationToken = default);
}

/// <summary>Senkron logu, iş durumları ve hatalı outbox kayıtlarının yeniden kuyruğa alınması (yönetici ekranı).</summary>
public class MarketplaceSyncAdminService(
    IRepository<MarketplaceSyncLog> logs,
    IRepository<MarketplaceSyncState> states,
    IRepository<MarketplaceInventoryChange> inventory,
    IRepository<MarketplaceOrderStatusUpdate> statusUpdates,
    IUnitOfWork unitOfWork) : IMarketplaceSyncAdminService
{
    public async Task<PagedResult<SyncLogDto>> ListLogsAsync(SyncLogQuery query, CancellationToken cancellationToken = default)
    {
        var (page, size) = Normalize(query.Page, query.Size);
        IQueryable<MarketplaceSyncLog> Filter(IQueryable<MarketplaceSyncLog> q) => q
            .Where(l => query.Marketplace == null || l.Marketplace == query.Marketplace)
            .Where(l => query.Operation == null || l.Operation == query.Operation)
            .Where(l => query.Succeeded == null || l.Succeeded == query.Succeeded);

        var total = await logs.CountAsync(Filter, cancellationToken);
        var items = await logs.ListAsync(q => Filter(q).OrderByDescending(l => l.Id).Skip(page * size).Take(size)
            .Select(l => new SyncLogDto(l.Id, l.Marketplace, l.Operation, l.Reference, l.Succeeded, l.Message, l.OccurredAt)), cancellationToken);
        return new PagedResult<SyncLogDto>(page, size, total, items);
    }

    public async Task<IReadOnlyList<SyncStateDto>> ListStatesAsync(CancellationToken cancellationToken = default) =>
        await states.ListAsync(q => q.OrderBy(s => s.Marketplace).ThenBy(s => s.Job)
            .Select(s => new SyncStateDto(s.Marketplace, s.Job, s.Cursor, s.PausedUntil, s.LastRunAt, s.LastError)), cancellationToken);

    public async Task<PagedResult<OutboxEntryDto>> ListOutboxAsync(OutboxKind kind, OutboxStatus? status, int page, int size, CancellationToken cancellationToken = default)
    {
        (page, size) = Normalize(page, size);
        if (kind == OutboxKind.Inventory)
        {
            IQueryable<MarketplaceInventoryChange> Filter(IQueryable<MarketplaceInventoryChange> q) => q.Where(c => status == null || c.Status == status);
            var total = await inventory.CountAsync(Filter, cancellationToken);
            var items = await inventory.ListAsync(q => Filter(q).OrderByDescending(c => c.Id).Skip(page * size).Take(size), cancellationToken);
            return new PagedResult<OutboxEntryDto>(page, size, total, items.Select(ToDto).ToList());
        }

        IQueryable<MarketplaceOrderStatusUpdate> FilterStatus(IQueryable<MarketplaceOrderStatusUpdate> q) => q.Where(c => status == null || c.Status == status);
        var count = await statusUpdates.CountAsync(FilterStatus, cancellationToken);
        var updates = await statusUpdates.ListAsync(q => FilterStatus(q).OrderByDescending(c => c.Id).Skip(page * size).Take(size), cancellationToken);
        return new PagedResult<OutboxEntryDto>(page, size, count, updates.Select(ToDto).ToList());
    }

    public async Task<Result<OutboxEntryDto>> RetryAsync(OutboxKind kind, int id, CancellationToken cancellationToken = default)
    {
        MarketplaceOutboxMessage? message = kind == OutboxKind.Inventory
            ? await inventory.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            : await statusUpdates.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (message is null)
            return Result<OutboxEntryDto>.NotFound("Kayıt bulunamadı.");
        if (message.Status != OutboxStatus.Failed)
            return Result<OutboxEntryDto>.Conflict("Yalnızca hatalı kayıtlar tekrar denenebilir.");

        message.Requeue();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<OutboxEntryDto>.Success(ToDto(message));
    }

    public async Task<int> RetryAllFailedAsync(OutboxKind kind, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<MarketplaceOutboxMessage> failed = kind == OutboxKind.Inventory
            ? await inventory.ListAsync(c => c.Status == OutboxStatus.Failed, cancellationToken)
            : await statusUpdates.ListAsync(c => c.Status == OutboxStatus.Failed, cancellationToken);
        foreach (var message in failed)
            message.Requeue();

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return failed.Count;
    }

    private static (int Page, int Size) Normalize(int page, int size) => (Math.Max(page, 0), Math.Clamp(size, 1, 200));

    private static OutboxEntryDto ToDto(MarketplaceOutboxMessage message) => message switch
    {
        MarketplaceInventoryChange c => new(c.Id, OutboxKind.Inventory, $"variant:{c.VariantId}", null, c.Status, c.Attempts, c.NextAttemptAt, c.ProcessedAt, c.Error, c.CreatedDate),
        MarketplaceOrderStatusUpdate u => new(u.Id, OutboxKind.OrderStatus, $"{u.Marketplace}:{u.ExternalReference}", u.Action.ToString(), u.Status, u.Attempts,
            u.NextAttemptAt, u.ProcessedAt, u.Error, u.CreatedDate),
        _ => throw new ArgumentOutOfRangeException(nameof(message))
    };
}
