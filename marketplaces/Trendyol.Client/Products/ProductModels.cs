namespace Trendyol.Client.Products;

public sealed record ItemsRequest<T>(IReadOnlyList<T> Items);

public sealed record BatchRequestResponse(string BatchRequestId);

public sealed record TrendyolImage(string Url);

public sealed record TrendyolProductAttribute(long AttributeId, long? AttributeValueId = null, string? CustomAttributeValue = null);

public sealed record TrendyolDeliveryOption(int? DeliveryDuration);

public sealed record CreateProductItem
{
    public required string Barcode { get; init; }
    public required string Title { get; init; }
    public required string ProductMainId { get; init; }
    public required long BrandId { get; init; }
    public required long CategoryId { get; init; }
    public required int Quantity { get; init; }
    public required string Description { get; init; }
    public required decimal ListPrice { get; init; }
    public required decimal SalePrice { get; init; }
    public required int VatRate { get; init; }
    public required IReadOnlyList<TrendyolImage> Images { get; init; }
    public required IReadOnlyList<TrendyolProductAttribute> Attributes { get; init; }
    public string? StockCode { get; init; }
    public decimal? DimensionalWeight { get; init; }
    public string? Origin { get; init; }
    public string? LotNumber { get; init; }
    public long? ShipmentAddressId { get; init; }
    public long? ReturningAddressId { get; init; }
    public IReadOnlyList<string>? CargoProviders { get; init; }
    public TrendyolDeliveryOption? DeliveryOption { get; init; }
}

/// <summary>Onaylı ürün içerik güncellemesi; null alanlar gönderilmez (kısmi güncelleme).</summary>
public sealed record ApprovedContentUpdateItem
{
    public required long ContentId { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<TrendyolImage>? Images { get; init; }
    public IReadOnlyList<TrendyolProductAttribute>? Attributes { get; init; }
}

public sealed record UnapprovedProductUpdateItem
{
    public required string Barcode { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? ProductMainId { get; init; }
    public long? BrandId { get; init; }
    public long? CategoryId { get; init; }
    public string? StockCode { get; init; }
    public string? Origin { get; init; }
    public decimal? DimensionalWeight { get; init; }
    public int? VatRate { get; init; }
    public string? LotNumber { get; init; }
    public long? ShipmentAddressId { get; init; }
    public long? ReturningAddressId { get; init; }
    public IReadOnlyList<string>? CargoProviders { get; init; }
    public TrendyolDeliveryOption? DeliveryOption { get; init; }
    public IReadOnlyList<TrendyolImage>? Images { get; init; }
    public IReadOnlyList<TrendyolProductAttribute>? Attributes { get; init; }
}

public sealed record PriceInventoryItem(string Barcode, int? Quantity = null, decimal? SalePrice = null, decimal? ListPrice = null);

public sealed record DeleteProductItem(string Barcode);

public sealed record BatchRequestResult
{
    public string BatchRequestId { get; init; } = string.Empty;
    public string? Status { get; init; }
    public long CreationDate { get; init; }
    public long LastModification { get; init; }
    public string? SourceType { get; init; }
    public int ItemCount { get; init; }
    public int FailedItemCount { get; init; }
    public string? BatchRequestType { get; init; }
    public IReadOnlyList<BatchRequestResultItem>? Items { get; init; }
}

public sealed record BatchRequestResultItem(BatchRequestItemReference? RequestItem, string? Status, IReadOnlyList<string>? FailureReasons);

public sealed record BatchRequestItemReference(string? Barcode);

public sealed record ApprovedProductQuery
{
    public string? Barcode { get; init; }
    public IReadOnlyList<string>? Barcodes { get; init; }
    public long? ContentId { get; init; }
    public string? StockCode { get; init; }
    public string? ProductMainId { get; init; }
    public IReadOnlyList<long>? BrandIds { get; init; }
    public string? Status { get; init; }
    public DateTimeOffset? StartDate { get; init; }
    public DateTimeOffset? EndDate { get; init; }
    public string? DateQueryType { get; init; }
    public int Page { get; init; }
    public int Size { get; init; } = 50;
    public string? NextPageToken { get; init; }
}

public sealed record ApprovedProductPage(int Page, int Size, int TotalElements, int TotalPages, string? NextPageToken, IReadOnlyList<ApprovedProduct>? Content);

public sealed record ApprovedProduct
{
    public long ContentId { get; init; }
    public string? ProductMainId { get; init; }
    public TrendyolNamedRef? Brand { get; init; }
    public TrendyolNamedRef? Category { get; init; }
    public long? CreationDate { get; init; }
    public long? LastModifiedDate { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<TrendyolImage>? Images { get; init; }
    public IReadOnlyList<TrendyolAttributeValueRef>? Attributes { get; init; }
    public IReadOnlyList<ApprovedProductVariant>? Variants { get; init; }
}

public sealed record ApprovedProductVariant
{
    public long VariantId { get; init; }
    public string Barcode { get; init; } = string.Empty;
    public string? StockCode { get; init; }
    public bool OnSale { get; init; }
    public bool Locked { get; init; }
    public bool Archived { get; init; }
    public bool Blacklisted { get; init; }
    public bool HasViolation { get; init; }
    public int? VatRate { get; init; }
    public string? ProductUrl { get; init; }
    public TrendyolStock? Stock { get; init; }
    public TrendyolPrice? Price { get; init; }
    public IReadOnlyList<TrendyolAttributeValueRef>? Attributes { get; init; }
}

public sealed record TrendyolNamedRef(long Id, string? Name);

public sealed record TrendyolAttributeValueRef(long AttributeId, string? AttributeName, long? AttributeValueId, string? AttributeValue);

public sealed record TrendyolStock(int? Quantity, long? LastModifiedDate);

public sealed record TrendyolPrice(decimal? SalePrice, decimal? ListPrice);

public sealed record UnapprovedProductQuery
{
    public string? Barcode { get; init; }
    public IReadOnlyList<string>? Barcodes { get; init; }
    public string? StockCode { get; init; }
    public string? ProductMainId { get; init; }
    public IReadOnlyList<long>? BrandIds { get; init; }
    public string? Status { get; init; }
    public DateTimeOffset? StartDate { get; init; }
    public DateTimeOffset? EndDate { get; init; }
    public string? DateQueryType { get; init; }
    public int Page { get; init; }
    public int Size { get; init; } = 50;
    public string? NextPageToken { get; init; }
}

public sealed record UnapprovedProductPage(int Page, int Size, int TotalElements, int TotalPages, string? NextPageToken, IReadOnlyList<UnapprovedProduct>? Content);

public sealed record UnapprovedProduct
{
    public string Barcode { get; init; } = string.Empty;
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? ProductMainId { get; init; }
    public string? StockCode { get; init; }
    public string? Status { get; init; }
    public TrendyolNamedRef? Brand { get; init; }
    public TrendyolNamedRef? Category { get; init; }
    public int? Quantity { get; init; }
    public decimal? ListPrice { get; init; }
    public decimal? SalePrice { get; init; }
    public int? VatRate { get; init; }
    public bool DocNeeded { get; init; }
    public long? CreateDateTime { get; init; }
    public long? LastUpdateDate { get; init; }
    public IReadOnlyList<TrendyolImage>? Media { get; init; }
    public IReadOnlyList<TrendyolAttributeValueRef>? Attributes { get; init; }
    public IReadOnlyList<TrendyolRejectReason>? RejectReasonDetails { get; init; }
}

public sealed record TrendyolRejectReason(string? RejectReason, string? RejectReasonDetail);
