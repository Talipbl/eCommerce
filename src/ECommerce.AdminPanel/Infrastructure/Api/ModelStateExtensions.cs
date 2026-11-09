using ECommerce.AdminPanel.Api;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ECommerce.AdminPanel.Infrastructure;

public static class ModelStateExtensions
{
    /// <summary>
    /// API hatasını forma yazar: alan hataları ilgili alana, diğerleri özet hataya (boş anahtar).
    /// API alan adları request DTO'su ile aynıdır; form modeli DTO'yu bir özellik altında taşıyorsa <paramref name="prefix"/> verilir.
    /// </summary>
    public static ApiProblem AddApiProblem(this ModelStateDictionary modelState, ApiException exception, string? prefix = null)
    {
        var problem = ApiProblem.From(exception);

        foreach (var (field, messages) in problem.FieldErrors)
        {
            var key = string.IsNullOrEmpty(prefix) ? field : $"{prefix}.{field}";
            foreach (var message in messages)
                modelState.AddModelError(key, message);
        }

        if (problem.FieldErrors.Count == 0)
            modelState.AddModelError(string.Empty, SummaryMessage(problem));

        foreach (var message in problem.Messages)
            modelState.AddModelError(string.Empty, message);

        return problem;
    }

    private static string SummaryMessage(ApiProblem problem) => problem switch
    {
        { Status: StatusCodes.Status409Conflict } => "Kayıt başka bir işlemde değişti, sayfayı yenileyip tekrar deneyin.",
        { Status: StatusCodes.Status503ServiceUnavailable, RetryAfter: { } wait } =>
            $"{problem.Title} Yaklaşık {Math.Ceiling(wait.TotalSeconds)} saniye sonra tekrar deneyin.",
        { UpstreamStatus: { } upstream } => $"{problem.Title} (pazaryeri cevabı: {upstream})",
        { Detail.Length: > 0 } => $"{problem.Title} {problem.Detail}",
        _ => problem.Title
    };
}
