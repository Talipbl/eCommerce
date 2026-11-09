using ECommerce.AdminPanel.Api;

namespace ECommerce.AdminPanel.Infrastructure;

public static class TrendyolLabels
{
    public static string Listing(ListingStatus status) => status switch
    {
        ListingStatus.Draft => "Taslak",
        ListingStatus.Submitted => "Gönderildi",
        ListingStatus.AwaitingApproval => "Onay bekliyor",
        ListingStatus.Live => "Satışta",
        ListingStatus.Rejected => "Reddedildi",
        ListingStatus.Failed => "Hatalı",
        _ => status.ToString()
    };

    public static string ListingBadge(ListingStatus status) => status switch
    {
        ListingStatus.Live => "text-bg-success",
        ListingStatus.AwaitingApproval or ListingStatus.Submitted => "text-bg-info",
        ListingStatus.Rejected or ListingStatus.Failed => "text-bg-danger",
        _ => "text-bg-secondary"
    };

    /// <summary>Kabul edilmiş veya işlemdeki ürün yeniden oluşturulamaz (API reddeder); gönder düğmesi gösterilmez.</summary>
    public static bool CanPublish(ListingStatus? status) => status is null or ListingStatus.Draft or ListingStatus.Rejected or ListingStatus.Failed;

    public static string Batch(MarketplaceBatchStatus status) => status switch
    {
        MarketplaceBatchStatus.Pending => "Sırada",
        MarketplaceBatchStatus.InProgress => "İşleniyor",
        MarketplaceBatchStatus.Completed => "Tamamlandı",
        MarketplaceBatchStatus.Expired => "Süresi doldu",
        _ => status.ToString()
    };

    public static string BatchOperation(MarketplaceBatchOperation operation) => operation switch
    {
        MarketplaceBatchOperation.ProductCreate => "Ürün oluşturma",
        MarketplaceBatchOperation.ProductContentUpdate => "İçerik güncelleme",
        MarketplaceBatchOperation.UnapprovedProductUpdate => "Onaysız ürün güncelleme",
        MarketplaceBatchOperation.PriceInventory => "Fiyat ve stok",
        MarketplaceBatchOperation.ProductDelete => "Ürün silme",
        _ => operation.ToString()
    };
}
