using System.ComponentModel.DataAnnotations;

namespace ECommerce.AdminPanel.Models;

public sealed class CancelOrderForm
{
    [Required(ErrorMessage = "İptal sebebi girilmelidir.")]
    [Display(Name = "İptal sebebi")]
    public string Reason { get; set; } = string.Empty;
}
