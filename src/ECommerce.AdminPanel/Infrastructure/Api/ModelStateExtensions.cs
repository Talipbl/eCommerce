using ECommerce.AdminPanel.Api;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ECommerce.AdminPanel.Infrastructure;

public static class ModelStateExtensions
{
    /// <summary>
    /// API hatasını forma yazar: alan hataları ilgili alana, diğerleri özet hataya (boş anahtar).
    /// Form alan adları API'dekinden farklıysa <paramref name="mapKey"/>, form modeli bir önek altındaysa <paramref name="prefix"/> verilir.
    /// </summary>
    public static ApiProblem AddApiProblem(this ModelStateDictionary modelState, ApiException exception, string? prefix = null, Func<string, string>? mapKey = null)
    {
        var problem = ApiProblem.From(exception);

        foreach (var (field, messages) in problem.FieldErrors)
        {
            var name = mapKey?.Invoke(field) ?? field;
            var key = string.IsNullOrEmpty(prefix) ? name : $"{prefix}.{name}";
            foreach (var message in messages)
                modelState.AddModelError(key, message);
        }

        if (problem.FieldErrors.Count == 0)
            modelState.AddModelError(string.Empty, problem.Summary);

        foreach (var message in problem.Messages)
            modelState.AddModelError(string.Empty, message);

        return problem;
    }
}
