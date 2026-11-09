using ECommerce.AdminPanel.Api;
using ECommerce.AdminPanel.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.AdminPanel.Controllers;

/// <summary>Panel ekranlarının ortak tabanı: yönlendirme sonrası gösterilecek başarı ve hata bildirimleri.</summary>
public abstract class PanelController : Controller
{
    internal const string SuccessKey = "Flash.Success";
    internal const string ErrorKey = "Flash.Error";

    protected void FlashSuccess(string message) => TempData[SuccessKey] = message;

    /// <summary>Form dışı işlemlerde (silme, yayınlama) API hatasını bir sonraki sayfada gösterir.</summary>
    protected void FlashError(ApiException exception) => TempData[ErrorKey] = ApiProblem.From(exception).Describe();

    protected void FlashError(string message) => TempData[ErrorKey] = message;
}
