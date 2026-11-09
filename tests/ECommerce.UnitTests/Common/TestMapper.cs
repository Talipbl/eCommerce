using ECommerce.Application;
using Mapster;
using MapsterMapper;

namespace ECommerce.UnitTests.Common;

/// <summary>Uygulamanın gerçek eşleme config'iyle derlenmiş, testlerde paylaşılan mapper.</summary>
public static class TestMapper
{
    public static TypeAdapterConfig Config { get; } = Create();

    public static IMapper Instance { get; } = new Mapper(Config);

    private static TypeAdapterConfig Create()
    {
        var config = MappingRegistration.CreateConfig();
        config.Compile();
        return config;
    }
}
