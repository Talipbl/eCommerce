using ECommerce.Core.Domain;
using ECommerce.Ordering;

namespace ECommerce.Marketplaces;

public enum MarketplaceOrderAction
{
    Picking,
    Invoiced
}

/// <summary>Yerelde ilerletilen pazaryeri siparişinin durumunu pazaryerine bildiren outbox kaydı.</summary>
public class MarketplaceOrderStatusUpdate : MarketplaceOutboxMessage
{
    public string Marketplace { get; private set; } = null!;

    public int OrderId { get; private set; }

    public string ExternalReference { get; private set; } = null!;

    public MarketplaceOrderAction Action { get; private set; }

    public static MarketplaceOrderStatusUpdate For(Order order, MarketplaceOrderAction action, string marketplace)
    {
        if (order.Channel == OrderChannel.Web || order.ExternalReference is null)
            throw new DomainException("Durum bildirimi yalnızca pazaryeri siparişleri için yapılır.");

        return new MarketplaceOrderStatusUpdate { Marketplace = marketplace, OrderId = order.Id, ExternalReference = order.ExternalReference, Action = action };
    }
}
