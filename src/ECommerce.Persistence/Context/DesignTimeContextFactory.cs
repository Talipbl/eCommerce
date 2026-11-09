using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ECommerce.Persistence;

/// <summary>
/// "dotnet ef" komutları için kullanılır; bağlantı dizesi ECOMMERCE_CONNECTION ortam değişkeninden okunur.
/// </summary>
public class DesignTimeContextFactory : IDesignTimeDbContextFactory<ECommerceContext>
{
    public ECommerceContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ECOMMERCE_CONNECTION")
            ?? "Server=localhost,1433;Database=ECommerce;Trusted_Connection=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<ECommerceContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "dbo"))
            .Options;

        return new ECommerceContext(options);
    }
}
