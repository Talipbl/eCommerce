using ECommerce.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public class DefAdminConfiguration : IEntityTypeConfiguration<DefAdmin>
{
    public void Configure(EntityTypeBuilder<DefAdmin> entity)
    {
        entity.HasKey(e => e.Id).HasName("Def_Admins_pk");

        entity.ToTable("Def_Admins", "dbo");

        entity.Property(e => e.FirstName).HasMaxLength(25);
        entity.Property(e => e.IsEnable2Fa).HasColumnName("IsEnable2FA");
        entity.Property(e => e.LastLoginDate).HasColumnType("datetime");
        entity.Property(e => e.LastName).HasMaxLength(25);
        entity.Property(e => e.MailAdress).HasMaxLength(75);
        entity.Property(e => e.OtpsetupCode)
            .HasMaxLength(250)
            .HasColumnName("OTPSetupCode");
        entity.Property(e => e.PasswordHash).HasMaxLength(500);
        entity.Property(e => e.RegisterDate).HasColumnType("datetime");
        entity.Property(e => e.UserName).HasMaxLength(30);
        entity.Property(e => e.Role).HasMaxLength(30);
        entity.Property(e => e.IsActive).HasDefaultValue(true);
        entity.HasIndex(e => e.UserName).IsUnique();
    }
}
