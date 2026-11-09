using Mapster;

namespace ECommerce.Application;

/// <summary>Eşleme sırasında dışarıdan verilen değerler: <c>mapper.From(x).AddParameters(Media, ...)</c>.</summary>
internal static class MappingParameters
{
    public const string Media = "media";

    public static T Get<T>(string key) => (T)MapContext.Current!.Parameters[key];
}
