namespace Trendyol.Client;

public sealed record TrendyolResponse<T>(bool IsSuccess, int StatusCode, T? Data, TrendyolError? Error)
{
    public static TrendyolResponse<T> Success(int statusCode, T? data) => new(true, statusCode, data, null);

    public static TrendyolResponse<T> Failure(int statusCode, TrendyolError error) => new(false, statusCode, default, error);
}

public sealed record TrendyolError(string? Exception, IReadOnlyList<TrendyolErrorDetail> Errors, string RawBody)
{
    public IReadOnlyList<string> Messages =>
        Errors.Count > 0
            ? Errors.Select(e => e.Message ?? e.Key ?? e.ErrorCode ?? string.Empty).Where(m => m.Length > 0).ToList()
            : string.IsNullOrWhiteSpace(RawBody) ? [] : [RawBody];
}

public sealed record TrendyolErrorDetail(string? Key, string? Message, string? ErrorCode);

/// <summary>Gövdesi olmayan cevaplar için.</summary>
public sealed record TrendyolEmpty;
