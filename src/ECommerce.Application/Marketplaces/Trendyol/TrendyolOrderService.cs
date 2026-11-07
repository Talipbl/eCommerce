using FluentValidation;
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

public interface ITrendyolOrderService
{
    Task<MarketplaceResult<ShipmentPackagePage>> GetPackagesAsync(OrderPackagesQuery query, CancellationToken cancellationToken = default);
    Task<MarketplaceResult<bool>> StartPickingAsync(long packageId, PackageLinesRequest request, CancellationToken cancellationToken = default);
    Task<MarketplaceResult<bool>> MarkInvoicedAsync(long packageId, InvoicePackageRequest request, CancellationToken cancellationToken = default);
    Task<MarketplaceResult<bool>> ReportUnsuppliedAsync(long packageId, UnsuppliedPackageRequest request, CancellationToken cancellationToken = default);
    Task<MarketplaceResult<bool>> SendInvoiceLinkAsync(long packageId, SendInvoiceLinkCommand command, CancellationToken cancellationToken = default);
    Task<MarketplaceResult<bool>> DeleteInvoiceLinkAsync(long packageId, long customerId, CancellationToken cancellationToken = default);
}

public class TrendyolOrderService(ITrendyolOrderClient client, TimeProvider clock) : ITrendyolOrderService
{
    public const int MaxPageSize = 200;
    public const int MaxAccessibleRecords = 10000;
    public static readonly TimeSpan MaxDateRange = TimeSpan.FromDays(14);

    public async Task<MarketplaceResult<ShipmentPackagePage>> GetPackagesAsync(OrderPackagesQuery query, CancellationToken cancellationToken = default)
    {
        var error = Validate(query);
        if (error is not null)
            return MarketplaceResult<ShipmentPackagePage>.Failure(MarketplaceError.Validation(error));

        var response = await client.GetShipmentPackagesAsync(new ShipmentPackageQuery
        {
            StartDate = query.StartDate,
            EndDate = query.EndDate,
            Status = query.Status,
            OrderNumber = query.OrderNumber,
            ShipmentPackageId = query.ShipmentPackageId,
            OrderByDirection = query.OrderByDirection,
            Page = query.Page,
            Size = query.Size
        }, cancellationToken);

        return response.ToMarketplaceResult();
    }

    public async Task<MarketplaceResult<bool>> StartPickingAsync(long packageId, PackageLinesRequest request, CancellationToken cancellationToken = default) =>
        (await client.UpdatePackageStatusAsync(packageId,
            new UpdatePackageStatusRequest(request.Lines, ShipmentPackageStatuses.Picking, new Dictionary<string, string>()), cancellationToken))
        .ToMarketplaceResult(_ => true);

    public async Task<MarketplaceResult<bool>> MarkInvoicedAsync(long packageId, InvoicePackageRequest request, CancellationToken cancellationToken = default) =>
        (await client.UpdatePackageStatusAsync(packageId,
            new UpdatePackageStatusRequest(request.Lines, ShipmentPackageStatuses.Invoiced, new Dictionary<string, string> { ["invoiceNumber"] = request.InvoiceNumber }),
            cancellationToken))
        .ToMarketplaceResult(_ => true);

    public async Task<MarketplaceResult<bool>> ReportUnsuppliedAsync(long packageId, UnsuppliedPackageRequest request, CancellationToken cancellationToken = default) =>
        (await client.ReportUnsuppliedAsync(packageId, new UnsuppliedRequest(request.Lines, (int)request.Reason), cancellationToken))
        .ToMarketplaceResult(_ => true);

    public async Task<MarketplaceResult<bool>> SendInvoiceLinkAsync(long packageId, SendInvoiceLinkCommand command, CancellationToken cancellationToken = default) =>
        (await client.SendInvoiceLinkAsync(new SendInvoiceLinkRequest(
            command.InvoiceLink, packageId, command.InvoiceDate?.ToUnixTimeMilliseconds(), command.InvoiceNumber), cancellationToken))
        .ToMarketplaceResult(_ => true);

    public async Task<MarketplaceResult<bool>> DeleteInvoiceLinkAsync(long packageId, long customerId, CancellationToken cancellationToken = default) =>
        (await client.DeleteInvoiceLinkAsync(new DeleteInvoiceLinkRequest(packageId, customerId), cancellationToken))
        .ToMarketplaceResult(_ => true);

    private string? Validate(OrderPackagesQuery query)
    {
        if (query.Size is < 1 or > MaxPageSize)
            return $"Sayfa boyutu 1-{MaxPageSize} arasında olmalıdır.";
        if (query.Page < 0 || (query.Page + 1) * query.Size > MaxAccessibleRecords)
            return $"Trendyol en fazla {MaxAccessibleRecords} kayda sayfalı erişim veriyor; filtreyi daraltın.";
        if (query.Status is not null && !ShipmentPackageStatuses.All.Contains(query.Status))
            return $"Geçersiz paket statüsü: {query.Status}.";
        if (query.StartDate is { } start && query.EndDate is { } end)
        {
            if (end < start)
                return "Bitiş tarihi başlangıç tarihinden önce olamaz.";
            if (end - start > MaxDateRange)
                return "Tarih aralığı en fazla 14 gün olabilir.";
        }
        if (query.StartDate < clock.GetUtcNow().AddMonths(-1))
            return "Trendyol en fazla son bir ayın siparişlerini döndürür.";

        return null;
    }
}

public class PackageLinesRequestValidator : AbstractValidator<PackageLinesRequest>
{
    public PackageLinesRequestValidator() => RuleFor(x => x.Lines).PackageLines();
}

public class InvoicePackageRequestValidator : AbstractValidator<InvoicePackageRequest>
{
    public InvoicePackageRequestValidator()
    {
        RuleFor(x => x.Lines).PackageLines();
        RuleFor(x => x.InvoiceNumber).NotEmpty().MaximumLength(50);
    }
}

public class UnsuppliedPackageRequestValidator : AbstractValidator<UnsuppliedPackageRequest>
{
    public UnsuppliedPackageRequestValidator()
    {
        RuleFor(x => x.Lines).PackageLines();
        RuleFor(x => x.Reason).IsInEnum();
    }
}

public class SendInvoiceLinkCommandValidator : AbstractValidator<SendInvoiceLinkCommand>
{
    public SendInvoiceLinkCommandValidator()
    {
        RuleFor(x => x.InvoiceLink).NotEmpty()
            .Must(l => Uri.TryCreate(l, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
            .WithMessage("Fatura linki https olmalıdır.");
        RuleFor(x => x.InvoiceNumber!)
            .Matches("^[A-Za-z0-9]{3}20[2-9][0-9][0-9]{9}$")
            .When(x => x.InvoiceNumber is not null)
            .WithMessage("Fatura numarası 3 karakter + yıl (2020-2099) + 9 haneli sıra numarası biçiminde olmalıdır.");
    }
}

internal static class PackageLineRules
{
    public static IRuleBuilderOptions<T, IReadOnlyList<PackageLine>> PackageLines<T>(this IRuleBuilder<T, IReadOnlyList<PackageLine>> rule) =>
        rule.NotEmpty()
            .Must(lines => lines.All(l => l.LineId > 0 && l.Quantity > 0)).WithMessage("Satır numarası ve adet pozitif olmalıdır.")
            .Must(lines => lines.Select(l => l.LineId).Distinct().Count() == lines.Count).WithMessage("Aynı satır birden fazla gönderilemez.");
}
