using ECommerce.AdminPanel.Api;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.AdminPanel.Models;

/// <summary>
/// Kargoya verme formu. Site siparişinde kargo firması ve takip numarası, pazaryeri siparişinde fatura numarası zorunludur;
/// kural API'de (domain) olduğu için burada tekrar edilmez, hata forma yazılır.
/// </summary>
public sealed class ShipOrderForm
{
    [Display(Name = "Kargo firması")]
    public string? CargoProvider { get; set; }

    [Display(Name = "Takip numarası")]
    public string? TrackingNumber { get; set; }

    [Display(Name = "Fatura numarası")]
    public string? InvoiceNumber { get; set; }

    public static ShipOrderForm From(OrderDto order) =>
        new() { CargoProvider = order.CargoProvider, TrackingNumber = order.TrackingNumber, InvoiceNumber = order.InvoiceNumber };

    public ShipOrderRequest ToRequest() => new()
    {
        CargoProvider = Normalize(CargoProvider),
        TrackingNumber = Normalize(TrackingNumber),
        InvoiceNumber = Normalize(InvoiceNumber)
    };

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
