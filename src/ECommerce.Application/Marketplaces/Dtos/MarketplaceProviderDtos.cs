using ECommerce.Marketplaces;
using ECommerce.Ordering;

namespace ECommerce.Application.Marketplaces;

public sealed record MarketplacePriceStock(string Barcode, int Quantity, decimal SalePrice, decimal ListPrice);

public sealed record MarketplaceOrderLine(long? ExternalLineId, string? Barcode, string? Sku, string ProductName, int Quantity, decimal UnitPrice, int VatRate);

public sealed record MarketplaceOrder(
    string ExternalReference,
    OrderStatus? Status,
    string CustomerEmail,
    string CustomerName,
    OrderAddress Address,
    IReadOnlyList<MarketplaceOrderLine> Lines,
    DateTime PlacedAt,
    string? CargoProvider,
    string? TrackingNumber);

public sealed record MarketplaceOrderPage(IReadOnlyList<MarketplaceOrder> Orders, int TotalPages);

public sealed record MarketplaceLineQuantity(long LineId, int Quantity);

public sealed record MarketplaceStatusCommand(string ExternalReference, MarketplaceOrderAction Action, IReadOnlyList<MarketplaceLineQuantity> Lines, string? InvoiceNumber);
