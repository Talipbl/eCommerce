using ECommerce.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace ECommerce.UnitTests.Persistence;

public static class PersistenceTestContext
{
    public static ECommerceContext Create(FakeTimeProvider clock, string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<ECommerceContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .AddInterceptors(new InventoryOutboxInterceptor(), new AuditSaveChangesInterceptor(clock))
            .Options;

        return new ECommerceContext(options);
    }
}
