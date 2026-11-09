using ECommerce.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace ECommerce.Persistence.Configurations;

internal static class CatalogTables
{
    // Soft-delete edilen kayıtlar tekillik kuralını engellemesin diye unique index'ler silinmemişlerle sınırlanır.
    public const string NotDeleted = "[DeletedDate] IS NULL";
}

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> entity)
    {
        entity.ToTable("Catalog_Categories", "dbo");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
        entity.Property(e => e.Slug).HasMaxLength(100).IsRequired();
        entity.Property(e => e.Description).HasMaxLength(1000);
        entity.HasIndex(e => e.Slug).IsUnique().HasFilter(CatalogTables.NotDeleted);
        entity.HasOne<Category>().WithMany().HasForeignKey(e => e.ParentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> entity)
    {
        entity.ToTable("Catalog_Brands", "dbo");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
        entity.Property(e => e.Slug).HasMaxLength(100).IsRequired();
        entity.HasIndex(e => e.Slug).IsUnique().HasFilter(CatalogTables.NotDeleted);
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> entity)
    {
        entity.ToTable("Catalog_Products", "dbo");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
        entity.Property(e => e.Slug).HasMaxLength(100).IsRequired();
        entity.Property(e => e.ProductCode).HasMaxLength(40).IsRequired();
        entity.HasIndex(e => e.Slug).IsUnique().HasFilter(CatalogTables.NotDeleted);
        entity.HasIndex(e => new { e.IsActive, e.CategoryId });

        entity.HasOne<Brand>().WithMany().HasForeignKey(e => e.BrandId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Category>().WithMany().HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.Restrict);

        entity.HasMany(e => e.Variants).WithOne().HasForeignKey(v => v.ProductId).OnDelete(DeleteBehavior.Cascade);
        entity.Navigation(e => e.Variants).UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();

        entity.HasMany(e => e.Images).WithOne().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Cascade);
        entity.Navigation(e => e.Images).UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
    }
}

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> entity)
    {
        entity.ToTable("Catalog_ProductVariants", "dbo");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Sku).HasMaxLength(100).IsRequired();
        entity.Property(e => e.Barcode).HasMaxLength(40).IsRequired();
        entity.Property(e => e.Price).HasPrecision(18, 2);
        entity.Property(e => e.ListPrice).HasPrecision(18, 2);
        entity.HasIndex(e => e.Sku).IsUnique().HasFilter(CatalogTables.NotDeleted);
        entity.HasIndex(e => e.Barcode).IsUnique().HasFilter(CatalogTables.NotDeleted);

        // Aynı stok için eşzamanlı siparişlerde fazla satışı önler.
        entity.Property<byte[]>("RowVersion").IsRowVersion();

        entity.Ignore(e => e.Attributes);
        entity.Property<List<VariantAttribute>>("_attributes")
            .HasColumnName("Attributes")
            .HasColumnType("nvarchar(max)")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Web),
                v => JsonSerializer.Deserialize<List<VariantAttribute>>(v, JsonSerializerOptions.Web) ?? new List<VariantAttribute>(),
                new ValueComparer<List<VariantAttribute>>(
                    (a, b) => (a ?? new List<VariantAttribute>()).SequenceEqual(b ?? new List<VariantAttribute>()),
                    v => v.Aggregate(0, (hash, item) => HashCode.Combine(hash, item)),
                    v => v.ToList()));
    }
}

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> entity)
    {
        entity.ToTable("Catalog_ProductImages", "dbo");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Path).HasMaxLength(300).IsRequired();
        entity.Property(e => e.AltText).HasMaxLength(200);
    }
}
