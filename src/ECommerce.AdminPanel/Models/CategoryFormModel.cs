using ECommerce.AdminPanel.Api;
using ECommerce.AdminPanel.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.AdminPanel.Models;

public sealed class CategoryFormModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Kategori adı zorunludur.")]
    [Display(Name = "Kategori adı")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Slug", Prompt = "Boş bırakılırsa addan üretilir")]
    public string? Slug { get; set; }

    [Display(Name = "Üst kategori")]
    public int? ParentId { get; set; }

    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Display(Name = "Sıra")]
    public int SortOrder { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    /// <summary>Seçilebilecek üst kategoriler; düzenlemede kategorinin kendisi ve alt kategorileri çıkarılır.</summary>
    [BindNever, ValidateNever]
    public IReadOnlyList<CategoryOption> ParentOptions { get; set; } = [];

    public static CategoryFormModel From(CategoryDto category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Slug = category.Slug,
        ParentId = category.ParentId,
        Description = category.Description,
        SortOrder = category.SortOrder,
        IsActive = category.IsActive
    };

    public SaveCategoryRequest ToRequest() => new()
    {
        Name = Name,
        Slug = string.IsNullOrWhiteSpace(Slug) ? null : Slug.Trim(),
        ParentId = ParentId,
        Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
        SortOrder = SortOrder,
        IsActive = IsActive
    };
}
