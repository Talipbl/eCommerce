using ECommerce.Core.Domain;

namespace ECommerce.Marketplaces;

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
