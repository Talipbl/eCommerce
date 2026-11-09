using ECommerce.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace ECommerce.IntegrationTests.Infrastructure;

/// <summary>
/// Testcontainers ile SQL Server başlatır ve migration'ları uygular. ECOMMERCE_TEST_SQLSERVER verilirse o sunucu kullanılır.
/// Docker yoksa yerelde testler atlanır; CI ortamında (CI=true) hata verir.
/// </summary>
public class SqlServerFixture : IAsyncLifetime
{
    private MsSqlContainer? _container;

    public string ConnectionString { get; private set; } = string.Empty;
    public string? UnavailableReason { get; private set; }

    public async ValueTask InitializeAsync()
    {
        // Docker yoksa mevcut bir sunucu kullanılabilir (ör. LocalDB); veritabanı her çalıştırmada silinip yeniden oluşturulur.
        if (Environment.GetEnvironmentVariable("ECOMMERCE_TEST_SQLSERVER") is { Length: > 0 } external)
        {
            ConnectionString = external;
            await using var existing = new ECommerceContext(Options(ConnectionString));
            await existing.Database.EnsureDeletedAsync();
            await existing.Database.MigrateAsync();
            return;
        }

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

        await using var context = new ECommerceContext(Options(ConnectionString));
        await context.Database.MigrateAsync();
    }

    private static DbContextOptions<ECommerceContext> Options(string connectionString) =>
        new DbContextOptionsBuilder<ECommerceContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "dbo"))
            .Options;

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
