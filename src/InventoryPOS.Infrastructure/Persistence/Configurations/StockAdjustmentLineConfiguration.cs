using InventoryPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryPOS.Infrastructure.Persistence.Configurations;

public class StockAdjustmentLineConfiguration
    : IEntityTypeConfiguration<StockAdjustmentLine>
{
    public void Configure(
        EntityTypeBuilder<StockAdjustmentLine> builder)
    {
        builder.ToTable("StockAdjustmentLines");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SystemQuantity)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(x => x.CountedQuantity)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(x => x.Difference)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.HasOne(x => x.ProductVariant)
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.StockAdjustmentId);

        builder.HasIndex(x => x.ProductVariantId);

        builder.HasIndex(
                x => new
                {
                    x.StockAdjustmentId,
                    x.ProductVariantId
                })
            .IsUnique();
    }
}