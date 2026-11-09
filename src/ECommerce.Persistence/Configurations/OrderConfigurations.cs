using ECommerce.Ordering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> entity)
    {
        entity.ToTable("Orders", "dbo");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.OrderNumber).HasMaxLength(30).IsRequired();
        entity.Property(e => e.ExternalReference).HasMaxLength(100);
        entity.Property(e => e.Channel).HasConversion<string>().HasMaxLength(20);
        entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        entity.Property(e => e.PaymentMethod).HasConversion<string>().HasMaxLength(20);
        entity.Property(e => e.CustomerEmail).HasMaxLength(150).IsRequired();
        entity.Property(e => e.CustomerName).HasMaxLength(150).IsRequired();
        entity.Property(e => e.Note).HasMaxLength(500);
        entity.Property(e => e.CargoProvider).HasMaxLength(100);
        entity.Property(e => e.TrackingNumber).HasMaxLength(100);
        entity.Property(e => e.InvoiceNumber).HasMaxLength(50);
        entity.Property(e => e.CancelReason).HasMaxLength(500);
        entity.Property(e => e.Subtotal).HasPrecision(18, 2);
        entity.Property(e => e.ShippingFee).HasPrecision(18, 2);
        entity.Property(e => e.PaymentFee).HasPrecision(18, 2);
        entity.Property(e => e.Total).HasPrecision(18, 2);

        entity.HasIndex(e => e.OrderNumber).IsUnique();
        entity.HasIndex(e => new { e.Channel, e.ExternalReference }).IsUnique().HasFilter("[ExternalReference] IS NOT NULL");
        entity.HasIndex(e => new { e.CustomerId, e.PlacedAt });
        entity.HasIndex(e => e.Status);

        entity.OwnsOne(e => e.ShippingAddress, address =>
        {
            address.Property(a => a.FullName).HasColumnName("ShipFullName").HasMaxLength(100);
            address.Property(a => a.Phone).HasColumnName("ShipPhone").HasMaxLength(20);
            address.Property(a => a.City).HasColumnName("ShipCity").HasMaxLength(50);
            address.Property(a => a.District).HasColumnName("ShipDistrict").HasMaxLength(50);
            address.Property(a => a.AddressLine).HasColumnName("ShipAddressLine").HasMaxLength(300);
            address.Property(a => a.PostalCode).HasColumnName("ShipPostalCode").HasMaxLength(10);
        });

        entity.HasMany(e => e.Lines).WithOne().HasForeignKey(l => l.OrderId).OnDelete(DeleteBehavior.Cascade);
        entity.Navigation(e => e.Lines).UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
        entity.HasMany(e => e.History).WithOne().HasForeignKey(h => h.OrderId).OnDelete(DeleteBehavior.Cascade);
        entity.Navigation(e => e.History).UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
    }
}

public class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> entity)
    {
        entity.ToTable("Order_Lines", "dbo");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.ProductName).HasMaxLength(200).IsRequired();
        entity.Property(e => e.VariantDescription).HasMaxLength(200);
        entity.Property(e => e.Barcode).HasMaxLength(40).IsRequired();
        entity.Property(e => e.Sku).HasMaxLength(100);
        entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
        entity.Property(e => e.LineTotal).HasPrecision(18, 2);
        entity.HasIndex(e => e.VariantId);
    }
}

public class OrderStatusChangeConfiguration : IEntityTypeConfiguration<OrderStatusChange>
{
    public void Configure(EntityTypeBuilder<OrderStatusChange> entity)
    {
        entity.ToTable("Order_StatusHistory", "dbo");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        entity.Property(e => e.Note).HasMaxLength(500);
    }
}
