using ECommerce.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace ECommerce.UnitTests.Persistence;

public class AuditSaveChangesInterceptorTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 6, 12, 0, 0, TimeSpan.Zero);
    private readonly FakeTimeProvider _clock = new(Now);

    [Fact]
    public async Task Added_entity_gets_created_date()
    {
        await using var context = PersistenceTestContext.Create(_clock);

        var brand = Brand.Create("Acme", "acme");
        context.Add(brand);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        brand.CreatedDate.ShouldBe(Now.UtcDateTime);
        brand.UpdatedDate.ShouldBeNull();
    }

    [Fact]
    public async Task Modified_entity_gets_updated_date()
    {
        await using var context = PersistenceTestContext.Create(_clock);
        var brand = Brand.Create("Acme", "acme");
        context.Add(brand);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        _clock.Advance(TimeSpan.FromHours(1));
        brand.Update("Acme Ltd", "acme-ltd");
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        brand.CreatedDate.ShouldBe(Now.UtcDateTime);
        brand.UpdatedDate.ShouldBe(Now.AddHours(1).UtcDateTime);
    }

    [Fact]
    public async Task Removed_entity_is_soft_deleted_and_hidden_from_queries()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var context = PersistenceTestContext.Create(_clock, databaseName))
        {
            var brand = Brand.Create("Acme", "acme");
            context.Add(brand);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            context.Remove(brand);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verify = PersistenceTestContext.Create(_clock, databaseName);
        (await verify.Brands.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(0);

        var deleted = await verify.Brands.IgnoreQueryFilters().SingleAsync(TestContext.Current.CancellationToken);
        deleted.DeletedDate.ShouldBe(Now.UtcDateTime);
    }
}
