namespace ECommerce.AdminPanel.Models;

public sealed record ErrorViewModel(int StatusCode, string RequestId)
{
    public string Title => StatusCode switch
    {
        StatusCodes.Status403Forbidden => "Bu sayfaya erişim yetkiniz yok.",
        StatusCodes.Status404NotFound => "Aradığınız sayfa bulunamadı.",
        _ => "Beklenmeyen bir hata oluştu."
    };
}
