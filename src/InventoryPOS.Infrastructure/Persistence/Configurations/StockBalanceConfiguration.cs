using InventoryPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryPOS.Infrastructure.Persistence.Configurations;

public class StockBalanceConfiguration :
    IEntityTypeConfiguration<StockBalance>
{
    public void Configure(
        EntityTypeBuilder<StockBalance> builder)
    {
        builder.ToTable("StockBalances");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.QuantityOnHand)
            .HasPrecision(18, 3);

        builder.Property(x => x.QuantityReserved)
            .HasPrecision(18, 3);

        builder.HasOne(x => x.ProductVariant)
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Location)
            .WithMany()
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(
                x => new
                {
                    x.ProductVariantId,
                    x.LocationId
                })
            .IsUnique();
    }
}