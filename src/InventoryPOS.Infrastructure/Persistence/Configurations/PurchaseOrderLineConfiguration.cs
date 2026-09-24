using InventoryPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryPOS.Infrastructure.Persistence.Configurations;

public class PurchaseOrderLineConfiguration
    : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(
        EntityTypeBuilder<PurchaseOrderLine> builder)
    {
        builder.ToTable("PurchaseOrderLines");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderedQuantity)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(x => x.ReceivedQuantity)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(x => x.UnitCost)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Ignore(x => x.LineTotal);

        builder.Ignore(x => x.RemainingQuantity);

        builder.HasOne(x => x.ProductVariant)
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.PurchaseOrderId);

        builder.HasIndex(x => x.ProductVariantId);

        builder.HasIndex(
                x => new
                {
                    x.PurchaseOrderId,
                    x.ProductVariantId
                })
            .IsUnique();
        builder.Property(x => x.WarrantyMonths);
    }
}