using InventoryPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryPOS.Infrastructure.Persistence.Configurations;

public sealed class WarrantyClaimEventConfiguration
    : IEntityTypeConfiguration<WarrantyClaimEvent>
{
    public void Configure(
        EntityTypeBuilder<WarrantyClaimEvent> builder)
    {
        builder.ToTable("WarrantyClaimEvents");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.FromStatus)
            .HasConversion<int>();

        builder.Property(x => x.ToStatus)
            .HasConversion<int>();

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Notes)
            .HasMaxLength(2000);

        builder.HasOne(x => x.WarrantyClaim)
            .WithMany(x => x.Events)
            .HasForeignKey(x => x.WarrantyClaimId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.WarrantyClaimId);

        builder.HasIndex(x => new
        {
            x.WarrantyClaimId,
            x.CreatedAtUtc
        });

        builder.HasIndex(x => x.EventType);

        builder.HasIndex(x => x.UserId);
    }
}