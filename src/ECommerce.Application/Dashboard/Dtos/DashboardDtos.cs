using ECommerce.Application.Marketplaces;
using ECommerce.Ordering;

namespace ECommerce.Application.Dashboard;

public record DashboardSummaryDto(
    IReadOnlyList<OrderStatusCountDto> OrdersByStatus,
    int PendingBankTransfers,
    RevenueDto Today,
    RevenueDto Last7Days,
    LowStockDto LowStock,
    FailedOutboxDto FailedOutbox,
    IReadOnlyList<SyncStateDto> SyncAlerts);

public record OrderStatusCountDto(OrderStatus Status, int Count);

/// <summary>İptal edilmemiş siparişlerin <see cref="From"/> (UTC) anından itibaren toplamı.</summary>
public record RevenueDto(DateTime From, decimal Total, int OrderCount, IReadOnlyList<ChannelRevenueDto> ByChannel);

public record ChannelRevenueDto(OrderChannel Channel, decimal Total, int OrderCount);

/// <summary>Stoğu eşik değer ve altındaki aktif varyantlar; liste en düşük stoktan başlar ve kısaltılmış olabilir.</summary>
public record LowStockDto(int Threshold, int TotalCount, IReadOnlyList<LowStockVariantDto> Variants);

public record LowStockVariantDto(int ProductId, string ProductName, int VariantId, string Sku, string Barcode, int StockQuantity);

public record FailedOutboxDto(int Inventory, int OrderStatus);
