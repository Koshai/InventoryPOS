
using InventoryPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryPOS.Infrastructure.Persistence.Configurations;

public class WarrantyClaimItemConfiguration
    : IEntityTypeConfiguration<WarrantyClaimItem>
{
    public void Configure(
        EntityTypeBuilder<WarrantyClaimItem> builder)
    {
        builder.ToTable("WarrantyClaimItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Quantity)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(x => x.SerialNumber)
            .HasMaxLength(150);

        builder.Property(x => x.ReportedDefect)
            .HasMaxLength(2000);

        builder.Property(x => x.TechnicianNotes)
            .HasMaxLength(2000);

        builder.Property(x => x.ResolutionType)
            .HasConversion<int>()
            .IsRequired();

        builder.HasOne(x => x.SaleLine)
            .WithMany()
            .HasForeignKey(x => x.SaleLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.SaleLineId);
        builder.HasIndex(x => x.SerialNumber);
    }
}
