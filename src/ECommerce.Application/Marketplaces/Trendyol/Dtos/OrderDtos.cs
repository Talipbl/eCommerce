using Trendyol.Client.Orders;

namespace ECommerce.Application.Marketplaces.Trendyol;

public record OrderPackagesQuery(
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    string? Status,
    string? OrderNumber,
    long? ShipmentPackageId,
    int Page = 0,
    int Size = 50,
    string OrderByDirection = "DESC");

public record PackageLinesRequest(IReadOnlyList<PackageLine> Lines);

public record InvoicePackageRequest(IReadOnlyList<PackageLine> Lines, string InvoiceNumber);

public enum UnsuppliedReason
{
    OutOfStock = 500,
    Defective = 501,
    WrongPrice = 502,
    IntegrationError = 504,
    BulkPurchase = 505,
    ForceMajeure = 506
}

public record UnsuppliedPackageRequest(IReadOnlyList<PackageLine> Lines, UnsuppliedReason Reason);

public record SendInvoiceLinkCommand(string InvoiceLink, string? InvoiceNumber, DateTimeOffset? InvoiceDate);
