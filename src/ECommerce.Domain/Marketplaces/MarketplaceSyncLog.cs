using ECommerce.Core.Domain;

namespace ECommerce.Marketplaces;

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
