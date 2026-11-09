namespace ECommerce.API.Infrastructure;

public class MediaOptions
{
    public const string SectionName = "Media";
    public const string RequestPath = "/media";

    /// <summary>Dosyaların yazılacağı klasör; göreliyse uygulama köküne göre çözülür.</summary>
    public string RootPath { get; set; } = "media";

    /// <summary>Dış dünyaya açık adres (ör. https://api.ornek.com). Trendyol mutlak https görsel adresi ister.</summary>
    public string PublicBaseUrl { get; set; } = string.Empty;
}
