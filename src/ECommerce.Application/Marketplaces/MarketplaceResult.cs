using Trendyol.Client;

namespace ECommerce.Application.Marketplaces;

public enum MarketplaceErrorKind
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    RateLimited,
    Unavailable
}

public sealed record MarketplaceError(MarketplaceErrorKind Kind, int? UpstreamStatus, IReadOnlyList<string> Messages)
{
    public static MarketplaceError Validation(params string[] messages) => new(MarketplaceErrorKind.Validation, null, messages);
}

public sealed record MarketplaceResult<T>(bool Succeeded, T? Value, MarketplaceError? Error)
{
    public static MarketplaceResult<T> Success(T value) => new(true, value, null);

    public static MarketplaceResult<T> Failure(MarketplaceError error) => new(false, default, error);
}

public static class MarketplaceResultExtensions
{
    public static MarketplaceResult<T> ToMarketplaceResult<T>(this TrendyolResponse<T> response) =>
        response.ToMarketplaceResult(value => value);

    public static MarketplaceResult<TOut> ToMarketplaceResult<TIn, TOut>(this TrendyolResponse<TIn> response, Func<TIn, TOut> map) =>
        response.IsSuccess
            ? MarketplaceResult<TOut>.Success(map(response.Data!))
            : MarketplaceResult<TOut>.Failure(ToError(response.StatusCode, response.Error));

    public static MarketplaceError ToError(int statusCode, TrendyolError? error) =>
        new(statusCode switch
        {
            400 or 422 => MarketplaceErrorKind.Validation,
            401 or 403 => MarketplaceErrorKind.Unauthorized,
            404 => MarketplaceErrorKind.NotFound,
            409 => MarketplaceErrorKind.Conflict,
            429 => MarketplaceErrorKind.RateLimited,
            _ => MarketplaceErrorKind.Unavailable
        }, statusCode, error?.Messages ?? []);
}
