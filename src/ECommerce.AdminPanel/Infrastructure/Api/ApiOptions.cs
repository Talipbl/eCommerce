using System.ComponentModel.DataAnnotations;

namespace ECommerce.AdminPanel.Infrastructure;

public sealed class ApiOptions
{
    public const string SectionName = "Api";

    /// <summary>API'nin iç ağ adresi (yayında http://api:8080/).</summary>
    [Required, Url]
    public string BaseUrl { get; set; } = string.Empty;

    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 30;
}
