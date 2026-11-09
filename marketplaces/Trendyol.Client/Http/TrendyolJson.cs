using System.Text.Json;
using System.Text.Json.Serialization;

namespace Trendyol.Client.Http;

internal static class TrendyolJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };
}
