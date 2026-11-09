using System.ComponentModel.DataAnnotations;

namespace ECommerce.AdminPanel.Models;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
    [Display(Name = "Kullanıcı adı")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Parola zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Parola")]
    public string Password { get; set; } = string.Empty;
}
