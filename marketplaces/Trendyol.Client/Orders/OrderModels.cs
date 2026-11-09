namespace Trendyol.Client.Orders;

public static class ShipmentPackageStatuses
{
    public const string Created = "Created";
    public const string Picking = "Picking";
    public const string Invoiced = "Invoiced";
    public const string Shipped = "Shipped";
    public const string Cancelled = "Cancelled";
    public const string Delivered = "Delivered";
    public const string UnDelivered = "UnDelivered";
    public const string Returned = "Returned";
    public const string AtCollectionPoint = "AtCollectionPoint";
    public const string UnPacked = "UnPacked";
    public const string UnSupplied = "UnSupplied";
    public const string Awaiting = "Awaiting";
    public const string Verified = "Verified";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Created, Picking, Invoiced, Shipped, Cancelled, Delivered, UnDelivered,
        Returned, AtCollectionPoint, UnPacked, UnSupplied, Awaiting, Verified
    };
}

public sealed record ShipmentPackageQuery
{
    public DateTimeOffset? StartDate { get; init; }
    public DateTimeOffset? EndDate { get; init; }
    public string? Status { get; init; }
    public string? OrderNumber { get; init; }
    public long? ShipmentPackageId { get; init; }
    public string? OrderByDirection { get; init; }
    public int Page { get; init; }
    public int Size { get; init; } = 50;
}

public sealed record ShipmentPackagePage(int Page, int Size, int TotalElements, int TotalPages, IReadOnlyList<ShipmentPackage>? Content);

public sealed record ShipmentPackage
{
    public long ShipmentPackageId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string? Status { get; init; }
    public string? ShipmentPackageStatus { get; init; }
    public long OrderDate { get; init; }
    public long? LastModifiedDate { get; init; }
    public long? AgreedDeliveryDate { get; init; }
    public long CustomerId { get; init; }
    public string? CustomerFirstName { get; init; }
    public string? CustomerLastName { get; init; }
    public string? CustomerEmail { get; init; }
    public string? CurrencyCode { get; init; }
    public decimal PackageGrossAmount { get; init; }
    public decimal PackageTotalDiscount { get; init; }
    public decimal PackageTotalPrice { get; init; }
    public string? CargoProviderName { get; init; }
    public long? CargoTrackingNumber { get; init; }
    public string? CargoTrackingLink { get; init; }
    public string? InvoiceLink { get; init; }
    public string? InvoiceNumber { get; init; }
    public bool FastDelivery { get; init; }
    public TrendyolAddress? ShipmentAddress { get; init; }
    public TrendyolAddress? InvoiceAddress { get; init; }
    public IReadOnlyList<ShipmentLine>? Lines { get; init; }
    public IReadOnlyList<PackageHistory>? PackageHistories { get; init; }
}

public sealed record ShipmentLine
{
    public long LineId { get; init; }
    public string? Barcode { get; init; }
    public string? StockCode { get; init; }
    public string? ProductName { get; init; }
    public long? ContentId { get; init; }
    public int Quantity { get; init; }
    public decimal LineUnitPrice { get; init; }
    public decimal LineGrossAmount { get; init; }
    public decimal LineTotalDiscount { get; init; }
    public decimal? VatRate { get; init; }
    public string? CurrencyCode { get; init; }
    public string? OrderLineItemStatusName { get; init; }
}

public sealed record TrendyolAddress
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? FullName { get; init; }
    public string? Company { get; init; }
    public string? FullAddress { get; init; }
    public string? City { get; init; }
    public string? District { get; init; }
    public string? PostalCode { get; init; }
    public string? CountryCode { get; init; }
    public string? Phone { get; init; }
    public string? TaxOffice { get; init; }
    public string? TaxNumber { get; init; }
}

public sealed record PackageHistory(long CreatedDate, string? Status);

public sealed record PackageLine(long LineId, int Quantity);

public sealed record UpdatePackageStatusRequest(IReadOnlyList<PackageLine> Lines, string Status, IReadOnlyDictionary<string, string> Params);

public sealed record UnsuppliedRequest(IReadOnlyList<PackageLine> Lines, int ReasonId);

public sealed record SendInvoiceLinkRequest(string InvoiceLink, long ShipmentPackageId, long? InvoiceDateTime = null, string? InvoiceNumber = null);

public sealed record DeleteInvoiceLinkRequest(long ServiceSourceId, long CustomerId, int ChannelId = 1);
