using InventoryPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryPOS.Infrastructure.Persistence.Configurations;

public class SalesReturnLineConfiguration :
    IEntityTypeConfiguration<SalesReturnLine>
{
    public void Configure(
        EntityTypeBuilder<SalesReturnLine> builder)
    {
        builder.ToTable("SalesReturnLines");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Quantity)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(x => x.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(x => x.DiscountAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.TaxAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.LineTotal)
            .HasPrecision(18, 2);

        builder.Property(x => x.Reason)
            .HasMaxLength(500);

        builder.HasIndex(x => x.SalesReturnId);

        builder.HasIndex(x => x.SaleLineId);

        builder.HasIndex(x => x.ProductVariantId);

        builder.HasOne(x => x.SalesReturn)
            .WithMany(x => x.Lines)
            .HasForeignKey(x => x.SalesReturnId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.SaleLine)
            .WithMany()
            .HasForeignKey(x => x.SaleLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ProductVariant)
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}