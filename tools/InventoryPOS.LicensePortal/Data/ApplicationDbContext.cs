using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InventoryPOS.LicensePortal.Data;

public sealed class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<MachineLicense> MachineLicenses => Set<MachineLicense>();
    public DbSet<IssueHistory> IssueHistories => Set<IssueHistory>();
    public DbSet<AppRelease> AppReleases => Set<AppRelease>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Customer>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(320).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Notes).HasMaxLength(4000);
        });

        builder.Entity<MachineLicense>(e =>
        {
            e.HasIndex(x => new { x.CustomerId, x.InstallationId }).IsUnique();
            e.Property(x => x.InstallationId).HasMaxLength(64).IsRequired();
            e.Property(x => x.Edition).HasMaxLength(64).IsRequired();
            e.Property(x => x.FeaturesJson).HasMaxLength(2000).IsRequired();
            e.Property(x => x.LastIssuedFileName).HasMaxLength(260);
            e.HasOne(x => x.Customer)
                .WithMany(x => x.Machines)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<IssueHistory>(e =>
        {
            e.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.Property(x => x.IssuedByUserName).HasMaxLength(256);
            e.HasOne(x => x.MachineLicense)
                .WithMany(x => x.History)
                .HasForeignKey(x => x.MachineLicenseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AppRelease>(e =>
        {
            e.HasIndex(x => x.Version).IsUnique();
            e.Property(x => x.Version).HasMaxLength(64).IsRequired();
            e.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            e.Property(x => x.Sha256).HasMaxLength(64).IsRequired();
            e.Property(x => x.Notes).HasMaxLength(4000);
            e.Property(x => x.PublishedByUserName).HasMaxLength(256);
        });
    }
}
