using System.Net.Http.Json;
using System.Text.Json;

namespace Trendyol.Client.Http;

internal sealed class TrendyolHttp(HttpClient httpClient)
{
    public Task<TrendyolResponse<T>> GetAsync<T>(string path, CancellationToken cancellationToken) =>
        SendAsync<T>(new HttpRequestMessage(HttpMethod.Get, path), cancellationToken);

    public Task<TrendyolResponse<T>> SendAsync<T>(HttpMethod method, string path, object body, CancellationToken cancellationToken) =>
        SendAsync<T>(new HttpRequestMessage(method, path) { Content = JsonContent.Create(body, body.GetType(), options: TrendyolJson.Options) }, cancellationToken);

    private async Task<TrendyolResponse<T>> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            return await SendCoreAsync<T>(request, cancellationToken);
        }
        catch (Exception ex) when (IsTransportFailure(ex, cancellationToken))
        {
            return TrendyolResponse<T>.Failure(503, new TrendyolError(ex.GetType().Name, [new TrendyolErrorDetail(null, "Trendyol'a ulaşılamadı: " + ex.Message, null)], string.Empty));
        }
    }

    // Ağ hataları, zaman aşımı ve devre kesici (Polly) reddi; çağıranın iptali hariç.
    private static bool IsTransportFailure(Exception ex, CancellationToken cancellationToken) =>
        ex is HttpRequestException or TimeoutException or Polly.ExecutionRejectedException
        || (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested);

    private async Task<TrendyolResponse<T>> SendCoreAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using (request)
        using (var response = await httpClient.SendAsync(request, cancellationToken))
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var status = (int)response.StatusCode;

            if (!response.IsSuccessStatusCode)
                return TrendyolResponse<T>.Failure(status, ParseError(body));

            if (typeof(T) == typeof(TrendyolEmpty) || string.IsNullOrWhiteSpace(body))
                return TrendyolResponse<T>.Success(status, default);

            return TrendyolResponse<T>.Success(status, JsonSerializer.Deserialize<T>(body, TrendyolJson.Options));
        }
    }

    internal static TrendyolError ParseError(string body)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<ErrorBody>(body, TrendyolJson.Options);
            if (parsed is not null)
                return new TrendyolError(parsed.Exception, parsed.Errors ?? [], body);
        }
        catch (JsonException)
        {
        }

        return new TrendyolError(null, [], body);
    }

    private sealed record ErrorBody(string? Exception, List<TrendyolErrorDetail>? Errors);
}
