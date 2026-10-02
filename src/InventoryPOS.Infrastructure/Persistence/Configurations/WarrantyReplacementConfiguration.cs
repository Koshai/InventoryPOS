
using InventoryPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryPOS.Infrastructure.Persistence.Configurations;

public sealed class WarrantyReplacementConfiguration
    : IEntityTypeConfiguration<WarrantyReplacement>
{
    public void Configure(
        EntityTypeBuilder<WarrantyReplacement> builder)
    {
        builder.ToTable("WarrantyReplacements", table =>
        {
            table.HasCheckConstraint(
                "CK_WarrantyReplacements_PositiveQuantity",
                "\"Quantity\" > 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Quantity)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(x => x.IssuedAtUtc)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(2000);

        builder.HasOne(x => x.WarrantyClaimItem)
            .WithMany()
            .HasForeignKey(x => x.WarrantyClaimItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReplacementVariant)
            .WithMany()
            .HasForeignKey(x => x.ReplacementVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Location)
            .WithMany()
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.InventoryBatch)
            .WithMany()
            .HasForeignKey(x => x.InventoryBatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.StockMovement)
            .WithMany()
            .HasForeignKey(x => x.StockMovementId)
            .OnDelete(DeleteBehavior.Restrict);

        // One ledger movement must not be assigned to
        // multiple replacement records.
        builder.HasIndex(x => x.StockMovementId)
            .IsUnique();

        builder.HasIndex(x => x.WarrantyClaimItemId);

        builder.HasIndex(x => new
        {
            x.ReplacementVariantId,
            x.LocationId
        });

        builder.HasIndex(x => x.InventoryBatchId);
    }
}
