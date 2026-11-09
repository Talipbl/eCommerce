using ECommerce.AdminPanel.Api;

namespace ECommerce.AdminPanel.Infrastructure;

public static class SyncLabels
{
    /// <summary>API'deki iş adları (MarketplaceSyncJobs) sabit metin; bilinmeyen ad olduğu gibi gösterilir.</summary>
    public static string Job(string job) => job switch
    {
        "OrderImport" => "Sipariş içe alma",
        "PriceInventory" => "Fiyat ve stok gönderimi",
        "OrderStatus" => "Sipariş durumu bildirimi",
        _ => job
    };

    public static string Operation(SyncOperation operation) => operation switch
    {
        SyncOperation.Publish => "Ürün gönderimi",
        SyncOperation.ListingStatus => "Ürün onay durumu",
        SyncOperation.PriceInventory => "Fiyat ve stok",
        SyncOperation.OrderImport => "Sipariş içe alma",
        SyncOperation.OrderStatus => "Sipariş durumu",
        _ => operation.ToString()
    };

    public static string Outbox(OutboxKind kind) => kind switch
    {
        OutboxKind.Inventory => "Fiyat ve stok",
        OutboxKind.OrderStatus => "Sipariş durumu",
        _ => kind.ToString()
    };

    public static string OutboxStatusLabel(OutboxStatus status) => status switch
    {
        OutboxStatus.Pending => "Bekliyor",
        OutboxStatus.Succeeded => "Gönderildi",
        OutboxStatus.Failed => "Hatalı",
        _ => status.ToString()
    };

    public static string OutboxBadge(OutboxStatus status) => status switch
    {
        OutboxStatus.Succeeded => "text-bg-success",
        OutboxStatus.Failed => "text-bg-danger",
        _ => "text-bg-secondary"
    };
}
