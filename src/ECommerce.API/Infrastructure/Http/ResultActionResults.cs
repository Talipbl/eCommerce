using ECommerce.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Infrastructure;

public static class ResultActionResults
{
    /// <summary>Başarıda 200 ile değeri döner. Dönüş tipi OpenAPI dokümanına cevap şeması olarak yazılır; client'lar buna göre üretilir.</summary>
    public static ActionResult<T> ToOk<T>(this ControllerBase controller, Result<T> result) =>
        result.Succeeded ? controller.Ok(result.Value) : controller.ToProblem(result.Error!);

    /// <summary>200 dışı başarı cevapları (201, 202, 204) için; cevap tipi action üzerinde <see cref="ProducesResponseTypeAttribute"/> ile belirtilmeli.</summary>
    public static IActionResult ToActionResult<T>(this ControllerBase controller, Result<T> result, Func<T, IActionResult> onSuccess) =>
        result.Succeeded ? onSuccess(result.Value!) : controller.ToProblem(result.Error!);

    private static ObjectResult ToProblem(this ControllerBase controller, AppError error)
    {
        var status = error.Kind switch
        {
            ErrorKind.NotFound => StatusCodes.Status404NotFound,
            ErrorKind.Conflict => StatusCodes.Status409Conflict,
            ErrorKind.Unavailable => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status400BadRequest
        };

        return controller.Problem(title: error.Message, statusCode: status);
    }
}
