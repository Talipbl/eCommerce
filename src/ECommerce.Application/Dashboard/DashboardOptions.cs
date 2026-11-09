namespace ECommerce.Application.Dashboard;

public class DashboardOptions
{
    public const string SectionName = "Dashboard";

    /// <summary>Stoğu bu değer ve altındaki varyantlar uyarı listesine girer.</summary>
    public int LowStockThreshold { get; set; } = 5;

    public int LowStockListSize { get; set; } = 20;

    /// <summary>"Bugün" ve "son 7 gün" bu saat dilimindeki gün başlangıcına göre hesaplanır.</summary>
    public string TimeZone { get; set; } = "Europe/Istanbul";
}
