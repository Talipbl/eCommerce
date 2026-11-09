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
