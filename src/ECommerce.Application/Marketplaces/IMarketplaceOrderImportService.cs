namespace ECommerce.Application.Marketplaces;

public interface IMarketplaceOrderImportService
{
    Task<IReadOnlyList<OrderImportSummary>> ImportAllAsync(CancellationToken cancellationToken = default);

    Task<OrderImportSummary> ImportAsync(string marketplace, CancellationToken cancellationToken = default);
}
