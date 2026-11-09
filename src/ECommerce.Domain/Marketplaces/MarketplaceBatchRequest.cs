using ECommerce.Core.Domain;

namespace ECommerce.Marketplaces;

public enum MarketplaceBatchOperation
{
    ProductCreate,
    ProductContentUpdate,
    UnapprovedProductUpdate,
    PriceInventory,
    ProductDelete
}

public enum MarketplaceBatchStatus
{
    Pending,
    InProgress,
    Completed,
    Expired
}

public sealed record MarketplaceBatchFailure(string? Barcode, IReadOnlyList<string> Reasons);

/// <summary>
/// Pazaryerine gönderilen asenkron toplu işlemin takibi. Trendyol sonuçları yaklaşık 4 saat saklar.
/// </summary>
public class MarketplaceBatchRequest : AggregateRoot<int>
{
    private readonly List<MarketplaceBatchFailure> _failures = [];

    public string Marketplace { get; private set; } = null!;

    public string BatchRequestId { get; private set; } = null!;

    public MarketplaceBatchOperation Operation { get; private set; }

    public MarketplaceBatchStatus Status { get; private set; }

    public int ItemCount { get; private set; }

    public int FailedItemCount { get; private set; }

    public DateTime? LastCheckedAt { get; private set; }

    public DateTime? CompletedAt { get; private set; }

    public IReadOnlyList<MarketplaceBatchFailure> Failures => _failures;

    public bool IsPending => Status is MarketplaceBatchStatus.Pending or MarketplaceBatchStatus.InProgress;

    public static MarketplaceBatchRequest Create(string marketplace, string batchRequestId, MarketplaceBatchOperation operation, int itemCount) =>
        new()
        {
            Marketplace = marketplace,
            BatchRequestId = batchRequestId,
            Operation = operation,
            ItemCount = itemCount,
            Status = MarketplaceBatchStatus.Pending
        };

    public void ApplyResult(bool completed, int itemCount, int failedItemCount, IEnumerable<MarketplaceBatchFailure> failures, DateTime now)
    {
        ItemCount = itemCount;
        FailedItemCount = failedItemCount;
        LastCheckedAt = now;
        _failures.Clear();
        _failures.AddRange(failures);

        Status = completed ? MarketplaceBatchStatus.Completed : MarketplaceBatchStatus.InProgress;
        CompletedAt = completed ? now : null;
    }

    public void MarkExpired(DateTime now)
    {
        Status = MarketplaceBatchStatus.Expired;
        LastCheckedAt = now;
    }
}
