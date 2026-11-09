using ECommerce.Ordering;

namespace ECommerce.Application.Ordering;

public class CheckoutOptions
{
    public const string SectionName = "Checkout";

    public decimal ShippingFee { get; set; } = 49.90m;
    public decimal FreeShippingThreshold { get; set; } = 750m;
    public decimal CashOnDeliveryFee { get; set; } = 19.90m;
    public BankTransferOptions BankTransfer { get; set; } = new();

    public decimal ShippingFeeFor(decimal subtotal) => subtotal >= FreeShippingThreshold ? 0m : ShippingFee;

    public decimal PaymentFeeFor(PaymentMethod method) => method == PaymentMethod.CashOnDelivery ? CashOnDeliveryFee : 0m;
}

public class BankTransferOptions
{
    public string BankName { get; set; } = string.Empty;
    public string AccountHolder { get; set; } = string.Empty;
    public string Iban { get; set; } = string.Empty;
}
