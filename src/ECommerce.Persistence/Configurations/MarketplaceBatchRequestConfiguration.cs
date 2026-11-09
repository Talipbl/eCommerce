using ECommerce.Marketplaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace ECommerce.Persistence.Configurations;

public class MarketplaceBatchRequestConfiguration : IEntityTypeConfiguration<MarketplaceBatchRequest>
{
    public void Configure(EntityTypeBuilder<MarketplaceBatchRequest> entity)
    {
        entity.HasKey(e => e.Id);

        entity.ToTable("Gnl_MarketplaceBatchRequests", "dbo");

        entity.Property(e => e.Marketplace).HasMaxLength(30).IsRequired();
        entity.Property(e => e.BatchRequestId).HasMaxLength(100).IsRequired();
        entity.Property(e => e.Operation).HasConversion<string>().HasMaxLength(40);
        entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        entity.HasIndex(e => new { e.Marketplace, e.BatchRequestId }).IsUnique();
        entity.HasIndex(e => e.Status);

        entity.Ignore(e => e.Failures);
        entity.Property<List<MarketplaceBatchFailure>>("_failures")
            .HasColumnName("Failures")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Web),
                v => JsonSerializer.Deserialize<List<MarketplaceBatchFailure>>(v, JsonSerializerOptions.Web) ?? new List<MarketplaceBatchFailure>(),
                new ValueComparer<List<MarketplaceBatchFailure>>(
                    (a, b) => JsonSerializer.Serialize(a, JsonSerializerOptions.Web) == JsonSerializer.Serialize(b, JsonSerializerOptions.Web),
                    v => JsonSerializer.Serialize(v, JsonSerializerOptions.Web).GetHashCode(),
                    v => v.ToList()))
            .HasColumnType("nvarchar(max)");
    }
}
