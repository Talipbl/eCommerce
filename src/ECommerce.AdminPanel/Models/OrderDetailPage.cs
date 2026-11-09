using ECommerce.AdminPanel.Api;

namespace ECommerce.AdminPanel.Models;

/// <summary>
/// Sipariş detayı ve duruma göre görünen aksiyonlar. <see cref="ActiveForm"/> hata alan formu işaretler;
/// özet hata yalnızca o formun altında gösterilir.
/// </summary>
public sealed record OrderDetailPage(OrderDto Order, ShipOrderForm Ship, CancelOrderForm Cancel, string? ActiveForm = null)
{
    public const string ShipForm = "ship";
    public const string CancelForm = "cancel";

    public bool IsMarketplace => Order.Channel != OrderChannel.Web;

    public bool CanConfirmPayment => Order.Status == OrderStatus.PendingPayment;

    public bool CanPrepare => Order.Status == OrderStatus.Confirmed;

    public bool CanShip => Order.Status == OrderStatus.Preparing;

    public bool CanDeliver => Order.Status == OrderStatus.Shipped;

    /// <summary>
    /// Pazaryeri siparişinin panelden iptali pazaryerine bildirilmiyor; iptal pazaryeri panelinden yapılır ve senkronla yansır.
    /// </summary>
    public bool CanCancel => !IsMarketplace && Order.Status is OrderStatus.PendingPayment or OrderStatus.Confirmed or OrderStatus.Preparing;

    public bool IsCancellableOnMarketplace => IsMarketplace && Order.Status is OrderStatus.Confirmed or OrderStatus.Preparing;
}
