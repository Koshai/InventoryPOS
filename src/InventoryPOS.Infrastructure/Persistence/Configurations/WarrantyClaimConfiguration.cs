
using InventoryPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryPOS.Infrastructure.Persistence.Configurations;

public class WarrantyClaimConfiguration
    : IEntityTypeConfiguration<WarrantyClaim>
{
    public void Configure(EntityTypeBuilder<WarrantyClaim> builder)
    {
        builder.ToTable("WarrantyClaims");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ClaimNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(x => x.ClaimNumber)
            .IsUnique();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.ClaimDate)
            .IsRequired();

        builder.Property(x => x.CustomerComplaint)
            .HasMaxLength(2000);

        builder.Property(x => x.InspectionNotes)
            .HasMaxLength(2000);

        builder.Property(x => x.ResolutionNotes)
            .HasMaxLength(2000);

        builder.HasOne(x => x.Sale)
            .WithMany()
            .HasForeignKey(x => x.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Location)
            .WithMany()
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Items)
            .WithOne(x => x.WarrantyClaim)
            .HasForeignKey(x => x.WarrantyClaimId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.SaleId);
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.ClaimDate);
        builder.HasIndex(x => x.Status);
    }
}
