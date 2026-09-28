using InventoryPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryPOS.Infrastructure.Persistence.Configurations;

public class SaleLineConfiguration :
    IEntityTypeConfiguration<SaleLine>
{
    public void Configure(
        EntityTypeBuilder<SaleLine> builder)
    {
        builder.ToTable("SaleLines");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Quantity)
            .HasPrecision(18, 3);

        builder.Property(x => x.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(x => x.DiscountAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.TaxAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.LineTotal)
            .HasPrecision(18, 2);

        builder.HasIndex(x => x.SaleId);

        builder.HasIndex(x => x.ProductVariantId);

        builder.HasOne(x => x.Sale)
            .WithMany(x => x.Lines)
            .HasForeignKey(x => x.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ProductVariant)
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.WarrantyMonths)
            .IsRequired(false);

        builder.Property(x => x.WarrantyStartDate)
            .IsRequired(false);

        builder.Property(x => x.WarrantyEndDate)
            .IsRequired(false);
    }
}