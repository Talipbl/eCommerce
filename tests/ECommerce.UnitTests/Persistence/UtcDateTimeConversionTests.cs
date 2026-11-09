using ECommerce.Ordering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace ECommerce.UnitTests.Persistence;

/// <summary>
/// SQL Server datetime2 değeri okunurken Kind=Unspecified döner; API bunu "Z" eki olmadan yazar ve client'lar yerel saat sanar.
/// Tüm DateTime alanları okumada UTC işaretlenmeli.
/// </summary>
public class UtcDateTimeConversionTests
{
    [Fact]
    public void Every_date_time_property_reads_back_as_utc()
    {
        using var context = PersistenceTestContext.Create(new FakeTimeProvider());
        var properties = context.Model.GetEntityTypes()
            .SelectMany(e => e.GetProperties())
            .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?))
            .ToList();

        properties.ShouldNotBeEmpty();
        properties.ShouldAllBe(p => p.GetValueConverter() != null);

        var placedAt = context.Model.FindEntityType(typeof(Order))!.FindProperty(nameof(Order.PlacedAt))!.GetValueConverter()!;
        var read = (DateTime)placedAt.ConvertFromProvider(new DateTime(2026, 11, 9, 6, 0, 0, DateTimeKind.Unspecified))!;
        read.Kind.ShouldBe(DateTimeKind.Utc);
        read.Hour.ShouldBe(6);
    }
}
