using ECommerce.Core.Domain;
using ECommerce.Core.Persistence;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Infrastructure;

public class GlobalExceptionHandler(IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // İş kuralı ihlallerinin mesajları kullanıcıya yöneliktir; diğer hataların ayrıntısı dışarı verilmez.
        var (status, title) = exception switch
        {
            DomainException => (StatusCodes.Status400BadRequest, exception.Message),
            ConcurrencyConflictException => (StatusCodes.Status409Conflict, exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "Beklenmeyen bir hata oluştu.")
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "İşlenmeyen hata: {Path}", httpContext.Request.Path);

        httpContext.Response.StatusCode = status;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Instance = httpContext.Request.Path
            }
        });
    }
}
