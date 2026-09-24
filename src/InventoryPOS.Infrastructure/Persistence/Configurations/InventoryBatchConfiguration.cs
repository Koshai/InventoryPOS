using InventoryPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryPOS.Infrastructure.Persistence.Configurations;

public class InventoryBatchConfiguration
    : IEntityTypeConfiguration<InventoryBatch>
{
    public void Configure(
        EntityTypeBuilder<InventoryBatch> builder)
    {
        builder.ToTable("InventoryBatches");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SupplierBatchNumber)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.ReceivedQuantity)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(x => x.QuantityOnHand)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(x => x.UnitCost)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasOne(x => x.ProductVariant)
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Location)
            .WithMany()
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.GoodsReceiptLine)
            .WithMany()
            .HasForeignKey(x => x.GoodsReceiptLineId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.ProductVariantId);

        builder.HasIndex(x => x.LocationId);

        builder.HasIndex(x => x.SupplierId);

        builder.HasIndex(x => x.SupplierBatchNumber);

        builder.HasIndex(x => x.ExpiryDate);

        builder.HasIndex(
            x => new
            {
                x.ProductVariantId,
                x.LocationId,
                x.SupplierBatchNumber
            });
    }
}