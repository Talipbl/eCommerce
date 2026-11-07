using ECommerce.Catalog;
using ECommerce.Core.Domain;
using ECommerce.Ordering;
using System.Globalization;

namespace ECommerce.Marketplaces;

public static class MarketplaceNames
{
    public const string Trendyol = "Trendyol";

    public static string? ForChannel(OrderChannel channel) => channel switch
    {
        OrderChannel.Trendyol => Trendyol,
        _ => null
    };
}

/// <summary>Yerel değer → pazaryeri değer numarası veya serbest metin.</summary>
public sealed record AttributeValueMapping(string LocalValue, long? ValueId, string? CustomValue);

/// <summary>
/// Pazaryeri kategori özelliğinin değer kaynağı. LocalAttributeName boşsa ürün seviyesinde sabit (varsayılan) değer kullanılır;
/// doluysa varyantın aynı adlı özelliği Values listesiyle çevrilir, eşleşmeyen değerde varsayılana düşülür.
/// </summary>
public sealed record CategoryAttributeMapping(
    long ExternalAttributeId,
    string? LocalAttributeName,
    long? DefaultValueId,
    string? DefaultCustomValue,
    IReadOnlyList<AttributeValueMapping> Values);

public class MarketplaceCategoryMapping : AggregateRoot<int>
{
    private static readonly CompareInfo Turkish = CultureInfo.GetCultureInfo("tr-TR").CompareInfo;

    private List<CategoryAttributeMapping> _attributes = [];

    public string Marketplace { get; private set; } = null!;

    public IReadOnlyList<CategoryAttributeMapping> Attributes => _attributes;

    public int CategoryId { get; private set; }

    public long ExternalCategoryId { get; private set; }

    public string? ExternalCategoryName { get; private set; }

    public static MarketplaceCategoryMapping Create(string marketplace, int categoryId, long externalCategoryId, string? externalCategoryName) =>
        new() { Marketplace = marketplace, CategoryId = categoryId, ExternalCategoryId = externalCategoryId, ExternalCategoryName = externalCategoryName };

    public void Update(long externalCategoryId, string? externalCategoryName)
    {
        // Kategori değişince eski kategorinin özellik numaraları geçersiz olur.
        if (externalCategoryId != ExternalCategoryId)
            _attributes = [];
        ExternalCategoryId = externalCategoryId;
        ExternalCategoryName = externalCategoryName;
    }

    public void SetAttributes(IEnumerable<CategoryAttributeMapping> attributes)
    {
        var list = attributes.ToList();
        if (list.Select(a => a.ExternalAttributeId).Distinct().Count() != list.Count)
            throw new DomainException("Aynı pazaryeri özelliği birden fazla kez eşlenemez.");
        if (list.Any(a => a.LocalAttributeName is null && !HasDefault(a)))
            throw new DomainException("Sabit özellik için değer numarası veya serbest değer girilmelidir.");
        if (list.Any(a => a.LocalAttributeName is not null && a.Values.Count == 0 && !HasDefault(a)))
            throw new DomainException("Varyant özelliği için en az bir değer eşlemesi veya varsayılan değer girilmelidir.");

        _attributes = list;
    }

    public IReadOnlyList<ListingAttribute> ResolveProductAttributes() =>
        _attributes.Where(a => a.LocalAttributeName is null).Select(Default).ToList();

    /// <summary>Varyant özelliklerini eşlemeye göre çevirir; karşılığı ve varsayılanı olmayan özellik atlanır.</summary>
    public IReadOnlyList<ListingAttribute> ResolveVariantAttributes(IEnumerable<VariantAttribute> variantAttributes)
    {
        var local = variantAttributes.ToList();
        var resolved = new List<ListingAttribute>();
        foreach (var mapping in _attributes.Where(a => a.LocalAttributeName is not null))
        {
            var value = local.FirstOrDefault(v => Same(v.Name, mapping.LocalAttributeName!))?.Value;
            var match = value is null ? null : mapping.Values.FirstOrDefault(v => Same(v.LocalValue, value));
            if (match is not null)
                resolved.Add(new ListingAttribute(mapping.ExternalAttributeId, match.ValueId, match.CustomValue));
            else if (HasDefault(mapping))
                resolved.Add(Default(mapping));
        }
        return resolved;
    }

    private static bool HasDefault(CategoryAttributeMapping a) => a.DefaultValueId is not null || !string.IsNullOrWhiteSpace(a.DefaultCustomValue);

    private static ListingAttribute Default(CategoryAttributeMapping a) => new(a.ExternalAttributeId, a.DefaultValueId, a.DefaultCustomValue);

    private static bool Same(string a, string b) => Turkish.Compare(a.Trim(), b.Trim(), CompareOptions.IgnoreCase) == 0;
}

public class MarketplaceBrandMapping : AggregateRoot<int>
{
    public string Marketplace { get; private set; } = null!;

    public int BrandId { get; private set; }

    public long ExternalBrandId { get; private set; }

    public string? ExternalBrandName { get; private set; }

    public static MarketplaceBrandMapping Create(string marketplace, int brandId, long externalBrandId, string? externalBrandName) =>
        new() { Marketplace = marketplace, BrandId = brandId, ExternalBrandId = externalBrandId, ExternalBrandName = externalBrandName };

    public void Update(long externalBrandId, string? externalBrandName)
    {
        ExternalBrandId = externalBrandId;
        ExternalBrandName = externalBrandName;
    }
}

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
