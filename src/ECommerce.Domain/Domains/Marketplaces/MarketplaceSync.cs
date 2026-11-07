using ECommerce.Core.Domain;
using ECommerce.Ordering;

namespace ECommerce.Marketplaces;

public enum OutboxStatus
{
    Pending,
    Succeeded,
    Failed
}

/// <summary>
/// Pazaryerine gönderilecek kaydın ortak tekrar deneme kuralları: geçici hatada artan bekleme, sınır aşılınca kalıcı hata.
/// </summary>
public abstract class MarketplaceOutboxMessage : AggregateRoot<int>
{
    public const int MaxAttempts = 5;

    public OutboxStatus Status { get; private set; }

    public int Attempts { get; private set; }

    public DateTime? NextAttemptAt { get; private set; }

    public DateTime? ProcessedAt { get; private set; }

    public string? Error { get; private set; }

    public bool IsDue(DateTime now) => Status == OutboxStatus.Pending && (NextAttemptAt is null || NextAttemptAt <= now);

    public void MarkSucceeded(DateTime now)
    {
        Status = OutboxStatus.Succeeded;
        ProcessedAt = now;
        NextAttemptAt = null;
        Error = null;
    }

    public void MarkFailed(string error, DateTime now)
    {
        Status = OutboxStatus.Failed;
        ProcessedAt = now;
        NextAttemptAt = null;
        Error = Truncate(error);
    }

    public void MarkTransientFailure(string error, DateTime now)
    {
        Attempts++;
        if (Attempts >= MaxAttempts)
        {
            MarkFailed(error, now);
            return;
        }

        Error = Truncate(error);
        NextAttemptAt = now.AddMinutes(Math.Pow(2, Attempts - 1));
    }

    public void Requeue()
    {
        if (Status != OutboxStatus.Failed)
            throw new DomainException("Yalnızca hatalı kayıtlar tekrar denenebilir.");

        Status = OutboxStatus.Pending;
        Attempts = 0;
        NextAttemptAt = null;
        ProcessedAt = null;
        Error = null;
    }

    private static string Truncate(string error) => error.Length <= 2000 ? error : error[..2000];
}

public enum MarketplaceOrderAction
{
    Picking,
    Invoiced
}

/// <summary>Yerelde ilerletilen pazaryeri siparişinin durumunu pazaryerine bildiren outbox kaydı.</summary>
public class MarketplaceOrderStatusUpdate : MarketplaceOutboxMessage
{
    public string Marketplace { get; private set; } = null!;

    public int OrderId { get; private set; }

    public string ExternalReference { get; private set; } = null!;

    public MarketplaceOrderAction Action { get; private set; }

    public static MarketplaceOrderStatusUpdate For(Order order, MarketplaceOrderAction action, string marketplace)
    {
        if (order.Channel == OrderChannel.Web || order.ExternalReference is null)
            throw new DomainException("Durum bildirimi yalnızca pazaryeri siparişleri için yapılır.");

        return new MarketplaceOrderStatusUpdate { Marketplace = marketplace, OrderId = order.Id, ExternalReference = order.ExternalReference, Action = action };
    }
}

public static class MarketplaceSyncJobs
{
    public const string OrderImport = "OrderImport";
    public const string PriceInventory = "PriceInventory";
    public const string OrderStatus = "OrderStatus";
}

/// <summary>Pazaryeri bazında arka plan işinin imleci (son çekim zamanı) ve rate limit sonrası bekleme süresi.</summary>
public class MarketplaceSyncState : AggregateRoot<int>
{
    public string Marketplace { get; private set; } = null!;

    public string Job { get; private set; } = null!;

    public DateTime? Cursor { get; private set; }

    public DateTime? PausedUntil { get; private set; }

    public DateTime? LastRunAt { get; private set; }

    public string? LastError { get; private set; }

    public static MarketplaceSyncState Create(string marketplace, string job) => new() { Marketplace = marketplace, Job = job };

    public bool IsPaused(DateTime now) => PausedUntil > now;

    public void PauseUntil(DateTime until) => PausedUntil = until;

    public void AdvanceCursor(DateTime to)
    {
        if (Cursor is null || to > Cursor)
            Cursor = to;
    }

    public void RecordRun(DateTime now, string? error)
    {
        LastRunAt = now;
        LastError = error is { Length: > 2000 } ? error[..2000] : error;
    }
}

public enum SyncOperation
{
    Publish,
    ListingStatus,
    PriceInventory,
    OrderImport,
    OrderStatus
}

/// <summary>Pazaryeriyle yapılan her senkron denemesinin kaydı; yönetici ekranında hata takibi için.</summary>
public class MarketplaceSyncLog : AggregateRoot<long>
{
    public string Marketplace { get; private set; } = null!;

    public SyncOperation Operation { get; private set; }

    public string? Reference { get; private set; }

    public bool Succeeded { get; private set; }

    public string? Message { get; private set; }

    public DateTime OccurredAt { get; private set; }

    public static MarketplaceSyncLog Write(string marketplace, SyncOperation operation, string? reference, bool succeeded, string? message, DateTime now) =>
        new()
        {
            Marketplace = marketplace,
            Operation = operation,
            Reference = reference,
            Succeeded = succeeded,
            Message = message is { Length: > 2000 } ? message[..2000] : message,
            OccurredAt = now
        };
}
