using ECommerce.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Infrastructure;

public static class ResultActionResults
{
    public static IActionResult ToActionResult<T>(this ControllerBase controller, Result<T> result, Func<T, IActionResult> onSuccess)
    {
        if (result.Succeeded)
            return onSuccess(result.Value!);

        var status = result.Error!.Kind switch
        {
            ErrorKind.NotFound => StatusCodes.Status404NotFound,
            ErrorKind.Conflict => StatusCodes.Status409Conflict,
            ErrorKind.Unavailable => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status400BadRequest
        };

        return controller.Problem(title: result.Error.Message, statusCode: status);
    }
}
