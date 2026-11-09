using ECommerce.Core.Domain;

namespace ECommerce.Marketplaces;

public enum ListingStatus
{
    Draft,
    Submitted,
    AwaitingApproval,
    Live,
    Rejected,
    Failed
}

public sealed record ListingAttribute(long AttributeId, long? ValueId, string? CustomValue);

public sealed record VariantListingAttributes(int VariantId, IReadOnlyList<ListingAttribute> Attributes);

/// <summary>
/// Yerel bir ürünün pazaryerindeki karşılığı: gönderilen özellikler, son batch ve onay durumu.
/// </summary>
public class MarketplaceListing : AggregateRoot<int>
{
    private List<ListingAttribute> _productAttributes = [];
    private List<VariantListingAttributes> _variantAttributes = [];

    public string Marketplace { get; private set; } = null!;

    public int ProductId { get; private set; }

    public ListingStatus Status { get; private set; }

    public long? ExternalContentId { get; private set; }

    public string? LastBatchRequestId { get; private set; }

    public string? StatusMessage { get; private set; }

    public int? DeliveryDuration { get; private set; }

    public DateTime? LastSubmittedAt { get; private set; }

    public DateTime? LastCheckedAt { get; private set; }

    public IReadOnlyList<ListingAttribute> ProductAttributes => _productAttributes;

    public IReadOnlyList<VariantListingAttributes> VariantAttributes => _variantAttributes;

    /// <summary>Stok/fiyat gönderimi yalnızca pazaryerinin kabul ettiği ürünler için yapılır.</summary>
    public bool IsSyncable => Status is ListingStatus.AwaitingApproval or ListingStatus.Live;

    public static MarketplaceListing Create(string marketplace, int productId) =>
        new() { Marketplace = marketplace, ProductId = productId, Status = ListingStatus.Draft };

    public void SetContent(IEnumerable<ListingAttribute> productAttributes, IEnumerable<VariantListingAttributes> variantAttributes, int? deliveryDuration)
    {
        _productAttributes = productAttributes.ToList();
        _variantAttributes = variantAttributes.ToList();
        DeliveryDuration = deliveryDuration;
    }

    public void MarkSubmitted(string batchRequestId, DateTime now)
    {
        if (Status is ListingStatus.Live or ListingStatus.AwaitingApproval)
            throw new DomainException("Pazaryerinde kabul edilmiş ürün yeniden yaratılamaz; güncelleme kullanın.");

        LastBatchRequestId = batchRequestId;
        LastSubmittedAt = now;
        StatusMessage = null;
        Status = ListingStatus.Submitted;
    }

    public void ApplyBatchResult(bool succeeded, IReadOnlyList<string> failures, DateTime now)
    {
        LastCheckedAt = now;
        Status = succeeded ? ListingStatus.AwaitingApproval : ListingStatus.Failed;
        StatusMessage = succeeded ? null : string.Join("; ", failures);
    }

    public void MarkLive(long contentId, DateTime now)
    {
        ExternalContentId = contentId;
        LastCheckedAt = now;
        StatusMessage = null;
        Status = ListingStatus.Live;
    }

    public void MarkRejected(string reason, DateTime now)
    {
        LastCheckedAt = now;
        StatusMessage = reason;
        Status = ListingStatus.Rejected;
    }
}
