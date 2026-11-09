using ECommerce.Core.Domain;

namespace ECommerce.Ordering;

public enum OrderChannel
{
    Web,
    Trendyol
}

public enum OrderStatus
{
    PendingPayment,
    Confirmed,
    Preparing,
    Shipped,
    Delivered,
    Cancelled
}

public enum PaymentMethod
{
    BankTransfer,
    CashOnDelivery,
    Marketplace
}

public sealed record OrderAddress(string FullName, string Phone, string City, string District, string AddressLine, string? PostalCode);

public sealed record OrderLineInput(
    int? VariantId,
    int? ProductId,
    string ProductName,
    string? VariantDescription,
    string Barcode,
    string? Sku,
    decimal UnitPrice,
    int Quantity,
    int VatRate,
    long? ExternalLineId = null);

/// <summary>
/// Sipariş aggregate'i ve durum makinesi. Satırlar sipariş anındaki ürün bilgisinin kopyasıdır.
/// </summary>
public class Order : AggregateRoot<int>
{
    private readonly List<OrderLine> _lines = [];
    private readonly List<OrderStatusChange> _history = [];

    public string OrderNumber { get; private set; } = null!;

    public OrderChannel Channel { get; private set; }

    public string? ExternalReference { get; private set; }

    public int? CustomerId { get; private set; }

    public string CustomerEmail { get; private set; } = null!;

    public string CustomerName { get; private set; } = null!;

    public OrderStatus Status { get; private set; }

    public PaymentMethod PaymentMethod { get; private set; }

    public OrderAddress ShippingAddress { get; private set; } = null!;

    public decimal Subtotal { get; private set; }

    public decimal ShippingFee { get; private set; }

    public decimal PaymentFee { get; private set; }

    public decimal Total { get; private set; }

    public string? Note { get; private set; }

    public string? CargoProvider { get; private set; }

    public string? TrackingNumber { get; private set; }

    public string? InvoiceNumber { get; private set; }

    public string? CancelReason { get; private set; }

    public DateTime PlacedAt { get; private set; }

    public DateTime? PaidAt { get; private set; }

    public DateTime? ShippedAt { get; private set; }

    public DateTime? DeliveredAt { get; private set; }

    public DateTime? CancelledAt { get; private set; }

    public IReadOnlyList<OrderLine> Lines => _lines;

    public IReadOnlyList<OrderStatusChange> History => _history;

    public bool CanCustomerCancel => Channel == OrderChannel.Web && Status is OrderStatus.PendingPayment or OrderStatus.Confirmed;

    public static Order PlaceWebOrder(
        string orderNumber, int customerId, string customerEmail, string customerName, OrderAddress address,
        PaymentMethod paymentMethod, IReadOnlyList<OrderLineInput> lines, decimal shippingFee, decimal paymentFee, string? note, DateTime now)
    {
        if (paymentMethod == PaymentMethod.Marketplace)
            throw new DomainException("Web siparişinde pazaryeri ödeme yöntemi kullanılamaz.");

        var order = Create(orderNumber, OrderChannel.Web, null, customerEmail, customerName, address, paymentMethod, lines, shippingFee, paymentFee, now);
        order.CustomerId = customerId;
        order.Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        order.SetStatus(paymentMethod == PaymentMethod.BankTransfer ? OrderStatus.PendingPayment : OrderStatus.Confirmed, now, null);
        return order;
    }

    /// <summary>Pazaryerinden gelen siparişi içe alır; ödeme pazaryerinde yapıldığı için onaylı başlar.</summary>
    public static Order ImportMarketplaceOrder(
        string orderNumber, OrderChannel channel, string externalReference, string customerEmail, string customerName,
        OrderAddress address, IReadOnlyList<OrderLineInput> lines, DateTime placedAt, string? cargoProvider = null, string? trackingNumber = null)
    {
        if (channel == OrderChannel.Web)
            throw new DomainException("Pazaryeri siparişi web kanalıyla içe alınamaz.");

        var order = Create(orderNumber, channel, externalReference, customerEmail, customerName, address, PaymentMethod.Marketplace, lines, 0m, 0m, placedAt);
        order.PaidAt = placedAt;
        order.CargoProvider = cargoProvider;
        order.TrackingNumber = trackingNumber;
        order.SetStatus(OrderStatus.Confirmed, placedAt, "Pazaryerinden içe alındı.");
        return order;
    }

    /// <summary>
    /// Pazaryerindeki durumu yansıtır; yalnızca ileri yönde ilerler. Durum değiştiyse true döner.
    /// </summary>
    public bool ApplyMarketplaceStatus(OrderStatus target, string? cargoProvider, string? trackingNumber, DateTime now)
    {
        if (Channel == OrderChannel.Web)
            throw new DomainException("Web siparişinin durumu pazaryerinden güncellenemez.");

        if (Status is OrderStatus.Cancelled or OrderStatus.Delivered || target == Status)
            return false;

        if (target == OrderStatus.Cancelled)
        {
            if (Status == OrderStatus.Shipped)
                return false;
            CancelReason = "Pazaryerinde iptal edildi.";
            CancelledAt = now;
            SetStatus(OrderStatus.Cancelled, now, CancelReason);
            return true;
        }

        if (Rank(target) <= Rank(Status))
            return false;

        if (target is OrderStatus.Shipped or OrderStatus.Delivered)
        {
            CargoProvider = cargoProvider ?? CargoProvider;
            TrackingNumber = trackingNumber ?? TrackingNumber;
            ShippedAt ??= now;
        }
        if (target == OrderStatus.Delivered)
            DeliveredAt = now;

        SetStatus(target, now, null);
        return true;
    }

    private static int Rank(OrderStatus status) => status switch
    {
        OrderStatus.PendingPayment => 0,
        OrderStatus.Confirmed => 1,
        OrderStatus.Preparing => 2,
        OrderStatus.Shipped => 3,
        OrderStatus.Delivered => 4,
        _ => -1
    };

    protected static Order Create(
        string orderNumber, OrderChannel channel, string? externalReference, string customerEmail, string customerName, OrderAddress address,
        PaymentMethod paymentMethod, IReadOnlyList<OrderLineInput> lines, decimal shippingFee, decimal paymentFee, DateTime now)
    {
        if (lines.Count == 0)
            throw new DomainException("Sipariş en az bir ürün içermelidir.");
        if (lines.Any(l => l.Quantity <= 0 || l.UnitPrice < 0))
            throw new DomainException("Sipariş satırlarında adet pozitif, fiyat negatif olmayan bir değer olmalıdır.");

        var order = new Order
        {
            OrderNumber = orderNumber,
            Channel = channel,
            ExternalReference = externalReference,
            CustomerEmail = customerEmail,
            CustomerName = customerName,
            ShippingAddress = address,
            PaymentMethod = paymentMethod,
            ShippingFee = shippingFee,
            PaymentFee = paymentFee,
            PlacedAt = now
        };
        order._lines.AddRange(lines.Select(l => new OrderLine(l)));
        order.Subtotal = order._lines.Sum(l => l.LineTotal);
        order.Total = order.Subtotal + shippingFee + paymentFee;
        return order;
    }

    public void ConfirmPayment(DateTime now)
    {
        Require(OrderStatus.PendingPayment, "Yalnızca ödeme bekleyen siparişin ödemesi onaylanabilir.");
        PaidAt = now;
        SetStatus(OrderStatus.Confirmed, now, "Ödeme alındı.");
    }

    public void StartPreparing(DateTime now)
    {
        Require(OrderStatus.Confirmed, "Yalnızca onaylanmış sipariş hazırlanmaya alınabilir.");
        SetStatus(OrderStatus.Preparing, now, null);
    }

    /// <summary>
    /// Kargoya verir. Pazaryeri siparişinde kargo pazaryerinin anlaşmalı firmasıdır (boşsa içe alınan değer kalır)
    /// ve pazaryerine bildirim için fatura numarası zorunludur.
    /// </summary>
    public void Ship(string? cargoProvider, string? trackingNumber, DateTime now, string? invoiceNumber = null)
    {
        Require(OrderStatus.Preparing, "Yalnızca hazırlanan sipariş kargoya verilebilir.");
        var cargo = string.IsNullOrWhiteSpace(cargoProvider) ? CargoProvider : cargoProvider.Trim();
        var tracking = string.IsNullOrWhiteSpace(trackingNumber) ? TrackingNumber : trackingNumber.Trim();
        if (Channel == OrderChannel.Web && (string.IsNullOrWhiteSpace(cargoProvider) || string.IsNullOrWhiteSpace(trackingNumber)))
            throw new DomainException("Kargo firması ve takip numarası girilmelidir.");
        if (Channel != OrderChannel.Web && string.IsNullOrWhiteSpace(invoiceNumber))
            throw new DomainException("Pazaryeri siparişi için fatura numarası girilmelidir.");

        CargoProvider = cargo;
        TrackingNumber = tracking;
        InvoiceNumber = string.IsNullOrWhiteSpace(invoiceNumber) ? InvoiceNumber : invoiceNumber.Trim();
        ShippedAt = now;
        SetStatus(OrderStatus.Shipped, now, $"{CargoProvider} - {TrackingNumber}");
    }

    public void Deliver(DateTime now)
    {
        Require(OrderStatus.Shipped, "Yalnızca kargodaki sipariş teslim edildi olarak işaretlenebilir.");
        DeliveredAt = now;
        SetStatus(OrderStatus.Delivered, now, null);
    }

    public void Cancel(string reason, DateTime now)
    {
        if (Status is not (OrderStatus.PendingPayment or OrderStatus.Confirmed or OrderStatus.Preparing))
            throw new DomainException("Kargoya verilmiş, teslim edilmiş veya iptal edilmiş sipariş iptal edilemez.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("İptal sebebi girilmelidir.");

        CancelReason = reason.Trim();
        CancelledAt = now;
        SetStatus(OrderStatus.Cancelled, now, CancelReason);
    }

    private void Require(OrderStatus expected, string message)
    {
        if (Status != expected)
            throw new DomainException(message);
    }

    private void SetStatus(OrderStatus status, DateTime now, string? note)
    {
        Status = status;
        _history.Add(new OrderStatusChange(status, now, note));
    }
}

public class OrderLine : AggregateRoot<int>
{
    private OrderLine()
    {
    }

    internal OrderLine(OrderLineInput input)
    {
        VariantId = input.VariantId;
        ProductId = input.ProductId;
        ProductName = input.ProductName;
        VariantDescription = input.VariantDescription;
        Barcode = input.Barcode;
        Sku = input.Sku;
        UnitPrice = input.UnitPrice;
        Quantity = input.Quantity;
        VatRate = input.VatRate;
        ExternalLineId = input.ExternalLineId;
        LineTotal = input.UnitPrice * input.Quantity;
    }

    public int OrderId { get; private set; }

    public int? VariantId { get; private set; }

    public int? ProductId { get; private set; }

    public string ProductName { get; private set; } = null!;

    public string? VariantDescription { get; private set; }

    public string Barcode { get; private set; } = null!;

    public string? Sku { get; private set; }

    public decimal UnitPrice { get; private set; }

    public int Quantity { get; private set; }

    public int VatRate { get; private set; }

    public decimal LineTotal { get; private set; }

    /// <summary>Pazaryerindeki satır numarası; durum bildirimlerinde gerekir.</summary>
    public long? ExternalLineId { get; private set; }
}

public class OrderStatusChange : AggregateRoot<int>
{
    private OrderStatusChange()
    {
    }

    internal OrderStatusChange(OrderStatus status, DateTime changedAt, string? note)
    {
        Status = status;
        ChangedAt = changedAt;
        Note = note;
    }

    public int OrderId { get; private set; }

    public OrderStatus Status { get; private set; }

    public DateTime ChangedAt { get; private set; }

    public string? Note { get; private set; }
}
