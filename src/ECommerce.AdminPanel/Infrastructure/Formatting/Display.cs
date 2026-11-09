using System.Globalization;

namespace ECommerce.AdminPanel.Infrastructure;

/// <summary>
/// Ekranda gösterim biçimleri (tr-TR, Europe/Istanbul). İstekler invariant kültürle işlendiği için
/// form değerleri her zaman nokta ondalıkla gelir; Türkçe biçim yalnızca gösterimde kullanılır.
/// </summary>
public static class Display
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly TimeZoneInfo Istanbul = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    /// <summary>Türkçe alfabeye göre sıralama (Ç, Ğ, İ, Ö, Ş, Ü doğru yerde).</summary>
    public static StringComparer TurkishOrder { get; } = StringComparer.Create(Turkish, ignoreCase: true);

    public static string Money(decimal? value) => value is null ? "—" : value.Value.ToString("C", Turkish);

    public static string Number(int value) => value.ToString("N0", Turkish);

    public static string Date(DateTimeOffset? value) =>
        value is null ? "—" : TimeZoneInfo.ConvertTime(value.Value, Istanbul).ToString("dd.MM.yyyy HH:mm", Turkish);

    public static string ActiveLabel(bool isActive) => isActive ? "Aktif" : "Pasif";
}
