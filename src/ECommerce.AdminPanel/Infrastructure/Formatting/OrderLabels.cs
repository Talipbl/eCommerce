using ECommerce.AdminPanel.Api;

namespace ECommerce.AdminPanel.Infrastructure;

/// <summary>Sipariş enum'larının ekrandaki Türkçe karşılıkları ve rozet renkleri.</summary>
public static class OrderLabels
{
    public static string Status(OrderStatus status) => status switch
    {
        OrderStatus.PendingPayment => "Ödeme bekliyor",
        OrderStatus.Confirmed => "Onaylandı",
        OrderStatus.Preparing => "Hazırlanıyor",
        OrderStatus.Shipped => "Kargoda",
        OrderStatus.Delivered => "Teslim edildi",
        OrderStatus.Cancelled => "İptal edildi",
        _ => status.ToString()
    };

    public static string StatusBadge(OrderStatus status) => status switch
    {
        OrderStatus.PendingPayment => "text-bg-warning",
        OrderStatus.Confirmed => "text-bg-primary",
        OrderStatus.Preparing => "text-bg-info",
        OrderStatus.Shipped => "text-bg-dark",
        OrderStatus.Delivered => "text-bg-success",
        _ => "text-bg-secondary"
    };

    public static string Channel(OrderChannel channel) => channel switch
    {
        OrderChannel.Web => "Site",
        OrderChannel.Trendyol => "Trendyol",
        _ => channel.ToString()
    };

    public static string Payment(PaymentMethod method) => method switch
    {
        PaymentMethod.BankTransfer => "Havale / EFT",
        PaymentMethod.CashOnDelivery => "Kapıda ödeme",
        PaymentMethod.Marketplace => "Pazaryeri",
        _ => method.ToString()
    };
}
