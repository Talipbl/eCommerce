using ECommerce.AdminPanel.Api;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.AdminPanel.Models;

public sealed class BrandFormModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Marka adı zorunludur.")]
    [Display(Name = "Marka adı")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Slug", Prompt = "Boş bırakılırsa addan üretilir")]
    public string? Slug { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    public static BrandFormModel From(BrandDto brand) => new() { Id = brand.Id, Name = brand.Name, Slug = brand.Slug, IsActive = brand.IsActive };

    public SaveBrandRequest ToRequest() => new() { Name = Name, Slug = string.IsNullOrWhiteSpace(Slug) ? null : Slug.Trim(), IsActive = IsActive };
}
