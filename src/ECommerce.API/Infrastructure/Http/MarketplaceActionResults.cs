using ECommerce.Application.Marketplaces;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Infrastructure;

public static class MarketplaceActionResults
{
    /// <summary>Başarıda 200 ile değeri döner; dönüş tipi OpenAPI dokümanına cevap şeması olarak yazılır.</summary>
    public static ActionResult<T> ToOk<T>(this ControllerBase controller, MarketplaceResult<T> result) =>
        result.Succeeded ? controller.Ok(result.Value) : controller.ToProblem(result.Error!);

    public static IActionResult ToActionResult<T>(this ControllerBase controller, MarketplaceResult<T> result, Func<T, IActionResult> onSuccess) =>
        result.Succeeded ? onSuccess(result.Value!) : controller.ToProblem(result.Error!);

    private static ObjectResult ToProblem(this ControllerBase controller, MarketplaceError error)
    {
        var (status, title) = error.Kind switch
        {
            MarketplaceErrorKind.Validation => (StatusCodes.Status400BadRequest, "İstek pazaryeri tarafından reddedildi."),
            MarketplaceErrorKind.NotFound => (StatusCodes.Status404NotFound, "Kayıt pazaryerinde bulunamadı."),
            MarketplaceErrorKind.Conflict => (StatusCodes.Status409Conflict, "İşlem pazaryerindeki mevcut kayıtla çakışıyor."),
            MarketplaceErrorKind.Unauthorized => (StatusCodes.Status502BadGateway, "Pazaryeri kimlik bilgileri reddedildi."),
            MarketplaceErrorKind.RateLimited => (StatusCodes.Status503ServiceUnavailable, "Pazaryeri istek limiti aşıldı, lütfen biraz sonra tekrar deneyin."),
            _ => (StatusCodes.Status502BadGateway, "Pazaryerine şu anda ulaşılamıyor.")
        };

        if (error.Kind == MarketplaceErrorKind.RateLimited)
            controller.Response.Headers.RetryAfter = "60";

        var problem = controller.ProblemDetailsFactory.CreateProblemDetails(controller.HttpContext, status, title);
        problem.Extensions["errors"] = error.Messages;
        if (error.UpstreamStatus is not null)
            problem.Extensions["upstreamStatus"] = error.UpstreamStatus;

        return new ObjectResult(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
