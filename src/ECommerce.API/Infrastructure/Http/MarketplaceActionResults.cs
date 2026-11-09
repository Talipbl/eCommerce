using ECommerce.Application.Marketplaces;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Infrastructure;

public static class MarketplaceActionResults
{
    public static IActionResult ToActionResult<T>(this ControllerBase controller, MarketplaceResult<T> result, Func<T, IActionResult> onSuccess)
    {
        if (result.Succeeded)
            return onSuccess(result.Value!);

        var error = result.Error!;
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
