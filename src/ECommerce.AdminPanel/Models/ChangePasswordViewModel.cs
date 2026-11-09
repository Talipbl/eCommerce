using System.ComponentModel.DataAnnotations;

namespace ECommerce.AdminPanel.Models;

/// <summary>Alan adları API'nin doğrulama hatası anahtarlarıyla aynıdır; hatalar ilgili alanın altına düşer.</summary>
public sealed class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Mevcut parola zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mevcut parola")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni parola zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni parola")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni parolayı tekrar girin.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Parolalar eşleşmiyor.")]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni parola (tekrar)")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
