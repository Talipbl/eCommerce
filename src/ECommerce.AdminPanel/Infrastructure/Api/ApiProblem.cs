using ECommerce.AdminPanel.Api;
using System.Text.Json;

namespace ECommerce.AdminPanel.Infrastructure;

/// <summary>
/// API'nin ProblemDetails cevabının panelde kullanılan kısmı. Doğrulama hataları alan bazında,
/// pazaryeri hataları ise alan adı olmadan <see cref="Messages"/> içinde gelir.
/// </summary>
public sealed record ApiProblem(
    int Status,
    string Title,
    string? Detail,
    IReadOnlyDictionary<string, IReadOnlyList<string>> FieldErrors,
    IReadOnlyList<string> Messages,
    int? UpstreamStatus,
    TimeSpan? RetryAfter)
{
    private const string DefaultTitle = "İşlem tamamlanamadı.";

    /// <summary>Alan dışı tek satırlık özet; pazaryeri hatalarında bekleme süresi veya cevap kodu eklenir. 409 iş kuralı da olabildiği için API başlığı aynen gösterilir.</summary>
    public string Summary => this switch
    {
        { Status: StatusCodes.Status503ServiceUnavailable, RetryAfter: { } wait } => $"{Title} Yaklaşık {Math.Ceiling(wait.TotalSeconds)} saniye sonra tekrar deneyin.",
        { UpstreamStatus: { } upstream } => $"{Title} (pazaryeri cevabı: {upstream})",
        { Detail.Length: > 0 } => $"{Title} {Detail}",
        _ => Title
    };

    /// <summary>Form dışı bildirimler için özet, pazaryeri mesajları ve alan hataları tek metinde.</summary>
    public string Describe() =>
        string.Join(" ", new[] { Summary }.Concat(Messages).Concat(FieldErrors.Values.SelectMany(m => m)));

    public static ApiProblem From(ApiException exception)
    {
        var fieldErrors = new Dictionary<string, IReadOnlyList<string>>();
        var messages = new List<string>();
        string? title = null, detail = null;
        int? upstreamStatus = null;

        if (TryParse(exception.Response, out var root))
        {
            title = ReadString(root, "title");
            detail = ReadString(root, "detail");
            if (root.TryGetProperty("upstreamStatus", out var upstream) && upstream.TryGetInt32(out var upstreamValue))
                upstreamStatus = upstreamValue;

            if (root.TryGetProperty("errors", out var errors))
            {
                // Doğrulama hataları nesne (alan → mesajlar), pazaryeri hataları düz dizi olarak gelir.
                if (errors.ValueKind == JsonValueKind.Object)
                    foreach (var field in errors.EnumerateObject())
                        fieldErrors[field.Name] = ReadStrings(field.Value);
                else if (errors.ValueKind == JsonValueKind.Array)
                    messages.AddRange(ReadStrings(errors));
            }
        }

        return new ApiProblem(
            exception.StatusCode,
            string.IsNullOrWhiteSpace(title) ? DefaultTitle : title,
            detail,
            fieldErrors,
            messages,
            upstreamStatus,
            ReadRetryAfter(exception.Headers));
    }

    private static bool TryParse(string? body, out JsonElement root)
    {
        root = default;
        if (string.IsNullOrWhiteSpace(body))
            return false;

        try
        {
            using var document = JsonDocument.Parse(body);
            root = document.RootElement.Clone();
            return root.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string? ReadString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static List<string> ReadStrings(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Array => value.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String).Select(e => e.GetString()!).ToList(),
        JsonValueKind.String => [value.GetString()!],
        _ => []
    };

    private static TimeSpan? ReadRetryAfter(IReadOnlyDictionary<string, IEnumerable<string>>? headers)
    {
        var value = headers?.FirstOrDefault(h => h.Key.Equals("Retry-After", StringComparison.OrdinalIgnoreCase)).Value?.FirstOrDefault();
        return int.TryParse(value, out var seconds) ? TimeSpan.FromSeconds(seconds) : null;
    }
}
