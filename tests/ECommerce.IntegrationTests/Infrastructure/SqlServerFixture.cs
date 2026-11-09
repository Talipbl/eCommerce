using ECommerce.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace ECommerce.IntegrationTests.Infrastructure;

/// <summary>
/// Testcontainers ile SQL Server başlatır ve migration'ları uygular.
/// Docker yoksa yerelde testler atlanır; CI ortamında (CI=true) hata verir.
/// </summary>
public class SqlServerFixture : IAsyncLifetime
{
    private MsSqlContainer? _container;

    public string ConnectionString { get; private set; } = string.Empty;
    public string? UnavailableReason { get; private set; }

    public async ValueTask InitializeAsync()
    {
        try
        {
            _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
            await _container.StartAsync();
        }
        catch (Exception ex) when (Environment.GetEnvironmentVariable("CI") is null)
        {
            UnavailableReason = $"Docker kullanılamıyor: {ex.Message}";
            return;
        }

        ConnectionString = _container.GetConnectionString();

        var options = new DbContextOptionsBuilder<ECommerceContext>()
            .UseSqlServer(ConnectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "dbo"))
            .Options;

        await using var context = new ECommerceContext(options);
        await context.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SqlServer";
}
