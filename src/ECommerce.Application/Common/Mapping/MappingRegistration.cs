using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Application;

public static class MappingRegistration
{
    public static TypeAdapterConfig CreateConfig()
    {
        var config = new TypeAdapterConfig();
        config.Scan(typeof(MappingRegistration).Assembly);
        return config;
    }

    public static IServiceCollection AddMappings(this IServiceCollection services)
    {
        var config = CreateConfig();
        config.Compile();

        services.AddSingleton(config);
        services.AddSingleton<IMapper>(sp => new Mapper(sp.GetRequiredService<TypeAdapterConfig>()));
        return services;
    }
}
