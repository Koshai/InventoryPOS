using InventoryPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryPOS.Infrastructure.Persistence.Configurations;

public class PurchaseRequisitionConfiguration
    : IEntityTypeConfiguration<PurchaseRequisition>
{
    public void Configure(
        EntityTypeBuilder<PurchaseRequisition> builder)
    {
        builder.ToTable("PurchaseRequisitions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RequisitionNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(x => x.RequisitionNumber)
            .IsUnique();

        builder.Property(x => x.EstimatedTotal)
            .HasPrecision(18, 2);

        builder.Property(x => x.Subject)
            .HasMaxLength(250);

        builder.Property(x => x.Notes)
            .HasMaxLength(2000);

        builder.Property(x => x.Terms)
            .HasMaxLength(2000);

        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Location)
            .WithMany()
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PurchaseOrder)
            .WithMany()
            .HasForeignKey(x => x.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Lines)
            .WithOne(x => x.PurchaseRequisition)
            .HasForeignKey(x => x.PurchaseRequisitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}