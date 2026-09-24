using InventoryPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryPOS.Infrastructure.Persistence.Configurations;

public sealed class BusinessProfileConfiguration
    : IEntityTypeConfiguration<BusinessProfile>
{
    public void Configure(EntityTypeBuilder<BusinessProfile> builder)
    {
        builder.ToTable("BusinessProfiles");

        builder.Property(x => x.BusinessName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.BusinessNameBn)
            .HasMaxLength(200);

        builder.Property(x => x.Address)
            .HasMaxLength(500);

        builder.Property(x => x.AddressBn)
            .HasMaxLength(500);

        builder.Property(x => x.Phone)
            .HasMaxLength(50);

        builder.Property(x => x.Email)
            .HasMaxLength(150);

        builder.Property(x => x.Website)
            .HasMaxLength(200);

        builder.Property(x => x.BinNumber)
            .HasMaxLength(100);

        builder.Property(x => x.TinNumber)
            .HasMaxLength(100);

        builder.Property(x => x.TradeLicenseNumber)
            .HasMaxLength(100);

        builder.Property(x => x.CurrencyCode)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.CurrencySymbol)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.LogoPath)
            .HasMaxLength(500);

        builder.Property(x => x.InvoiceFooter)
            .HasMaxLength(1000);
    }
}