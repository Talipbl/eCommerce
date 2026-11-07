using ECommerce.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ECommerce.Persistence;

public static class DependencyInjection
{
    public const string ConnectionStringName = "ECommerce";

    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException($"'ConnectionStrings:{ConnectionStringName}' tanımlı değil.");

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddSingleton<InventoryOutboxInterceptor>();

        services.AddDbContext<ECommerceContext>((provider, options) => options
            .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "dbo"))
            .AddInterceptors(provider.GetRequiredService<InventoryOutboxInterceptor>(), provider.GetRequiredService<AuditSaveChangesInterceptor>()));

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<ECommerceContext>());
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));

        return services;
    }
}
