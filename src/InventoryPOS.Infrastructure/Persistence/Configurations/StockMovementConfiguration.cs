using InventoryPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryPOS.Infrastructure.Persistence.Configurations;

public class StockMovementConfiguration :
    IEntityTypeConfiguration<StockMovement>
{
    public void Configure(
        EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Quantity)
            .HasPrecision(18, 3);

        builder.Property(x => x.UnitCost)
            .HasPrecision(18, 2);

        builder.Property(x => x.ReferenceType)
            .HasMaxLength(100);

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        builder.HasOne(x => x.ProductVariant)
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Location)
            .WithMany()
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ProductVariantId);

        builder.HasIndex(x => x.LocationId);

        builder.HasIndex(x => x.CreatedAtUtc);
        builder.HasOne(x => x.InventoryBatch)
            .WithMany()
            .HasForeignKey(x => x.InventoryBatchId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.InventoryBatchId);
    }
}