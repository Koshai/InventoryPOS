using InventoryPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryPOS.Infrastructure.Persistence.Configurations;

public class PurchaseRequisitionLineConfiguration
    : IEntityTypeConfiguration<PurchaseRequisitionLine>
{
    public void Configure(
        EntityTypeBuilder<PurchaseRequisitionLine> builder)
    {
        builder.ToTable("PurchaseRequisitionLines");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RequestedQuantity)
            .HasPrecision(18, 3);

        builder.Property(x => x.EstimatedUnitCost)
            .HasPrecision(18, 2);

        builder.Property(x => x.EstimatedLineTotal)
            .HasPrecision(18, 2);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.HasOne(x => x.ProductVariant)
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}