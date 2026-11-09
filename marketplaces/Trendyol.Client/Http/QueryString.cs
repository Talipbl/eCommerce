using System.Globalization;

namespace Trendyol.Client.Http;

/// <summary>
/// Trendyol query string kuralları: boolean küçük harf, tarihler Unix milisaniye, boş değerler atlanır.
/// </summary>
internal sealed class QueryString
{
    private readonly List<string> _parts = [];

    public QueryString Add(string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            _parts.Add($"{name}={Uri.EscapeDataString(value)}");
        return this;
    }

    public QueryString Add(string name, long? value) => Add(name, value?.ToString(CultureInfo.InvariantCulture));

    public QueryString Add(string name, bool? value) => Add(name, value is null ? null : value.Value ? "true" : "false");

    public QueryString Add(string name, DateTimeOffset? value) => Add(name, value?.ToUnixTimeMilliseconds());

    public QueryString Add(string name, IEnumerable<long>? values)
    {
        foreach (var value in values ?? [])
            Add(name, value);
        return this;
    }

    public QueryString AddCsv(string name, IEnumerable<string>? values)
    {
        var list = values?.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
        return list is { Count: > 0 } ? Add(name, string.Join(',', list)) : this;
    }

    public string Apply(string path) => _parts.Count == 0 ? path : $"{path}?{string.Join('&', _parts)}";
}
