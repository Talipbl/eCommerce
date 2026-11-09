using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Application;

public static class MappingRegistration
{
    public static TypeAdapterConfig CreateConfig()
    {
        // Her eşleme bir IRegister'da açıkça tanımlanır; eksik ya da kaynaksız alan Compile() sırasında hata verir.
        var config = new TypeAdapterConfig
        {
            RequireExplicitMapping = true,
            RequireDestinationMemberSource = true
        };
        config.Default.ShallowCopyForSameType(true);
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
