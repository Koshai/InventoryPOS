using InventoryPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryPOS.Infrastructure.Persistence;

public class InventoryPosDbContext : DbContext
{
    public InventoryPosDbContext(
        DbContextOptions<InventoryPosDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Brand> Brands => Set<Brand>();

    public DbSet<Unit> Units => Set<Unit>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductVariant> ProductVariants =>
        Set<ProductVariant>();
    public DbSet<Location> Locations =>
    Set<Location>();

    public DbSet<StockBalance> StockBalances =>
        Set<StockBalance>();

    public DbSet<StockMovement> StockMovements =>
        Set<StockMovement>();
    public DbSet<ProductImage> ProductImages =>
        Set<ProductImage>();
    public DbSet<StockAdjustment> StockAdjustments =>
    Set<StockAdjustment>();

    public DbSet<StockAdjustmentLine> StockAdjustmentLines =>
        Set<StockAdjustmentLine>();

    public DbSet<Supplier> Suppliers =>
    Set<Supplier>();

    public DbSet<PurchaseOrder> PurchaseOrders =>
        Set<PurchaseOrder>();

    public DbSet<PurchaseOrderLine> PurchaseOrderLines =>
        Set<PurchaseOrderLine>();

    public DbSet<GoodsReceipt> GoodsReceipts =>
        Set<GoodsReceipt>();

    public DbSet<GoodsReceiptLine> GoodsReceiptLines =>
        Set<GoodsReceiptLine>();
    public DbSet<InventoryBatch> InventoryBatches =>
        Set<InventoryBatch>();
    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Sale> Sales => Set<Sale>();

    public DbSet<SaleLine> SaleLines => Set<SaleLine>();

    public DbSet<SalePayment> SalePayments => Set<SalePayment>();
    public DbSet<BusinessProfile> BusinessProfiles => Set<BusinessProfile>();
    public DbSet<SalesReturn> SalesReturns => Set<SalesReturn>();

    public DbSet<SalesReturnLine> SalesReturnLines => Set<SalesReturnLine>();

    public DbSet<SalesRefund> SalesRefunds => Set<SalesRefund>();
    public DbSet<PurchaseReturn> PurchaseReturns => Set<PurchaseReturn>();

    public DbSet<PurchaseReturnLine> PurchaseReturnLines => Set<PurchaseReturnLine>();
    public DbSet<PurchaseRequisition> PurchaseRequisitions => Set<PurchaseRequisition>();

    public DbSet<PurchaseRequisitionLine> PurchaseRequisitionLines => Set<PurchaseRequisitionLine>();
    public DbSet<SalesQuote> SalesQuotes => Set<SalesQuote>();

    public DbSet<SalesQuoteLine> SalesQuoteLines => Set<SalesQuoteLine>();
    public DbSet<WarrantyClaim> WarrantyClaims => Set<WarrantyClaim>();

    public DbSet<WarrantyClaimItem> WarrantyClaimItems => Set<WarrantyClaimItem>();
    public DbSet<WarrantyReplacement> WarrantyReplacements => Set<WarrantyReplacement>();
    public DbSet<WarrantyClaimEvent> WarrantyClaimEvents => Set<WarrantyClaimEvent>();
    public DbSet<DocumentIdentifier> DocumentIdentifiers => Set<DocumentIdentifier>();
    public DbSet<ProductBarcode> ProductBarcodes => Set<ProductBarcode>();
    public DbSet<DocumentNumberCounter> DocumentNumberCounters => Set<DocumentNumberCounter>();
    public DbSet<CustomerPaymentOperation> CustomerPaymentOperations => Set<CustomerPaymentOperation>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(InventoryPosDbContext).Assembly);

        // DT6.4.3A — Sales return idempotency.
        modelBuilder.Entity<SalesReturn>(entity =>
        {
            entity.HasIndex(x => x.ReturnRequestId)
                .IsUnique()
                .HasFilter("\"ReturnRequestId\" IS NOT NULL");

            entity.Property(x => x.ReturnRequestFingerprint)
                .HasMaxLength(64)
                .IsUnicode(false);

            entity.Property(x => x.ReturnResultJson)
                .HasColumnType("text");
        });

        modelBuilder.Entity<Sale>()
            .HasIndex(x => x.CheckoutRequestId)
            .IsUnique()
            .HasFilter("\"CheckoutRequestId\" IS NOT NULL");

        modelBuilder.Entity<Sale>()
            .Property(x => x.CheckoutRequestFingerprint)
            .HasMaxLength(64)
            .IsUnicode(false);

        modelBuilder.Entity<DocumentNumberCounter>(entity =>
        {
            entity.Property(x => x.DocumentPrefix)
                .HasMaxLength(20)
                .IsRequired();

            entity.HasIndex(x => new
            {
                x.DocumentPrefix,
                x.DocumentDate
            }).IsUnique();
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.HasIndex(x => x.InvoiceNumber)
                .IsUnique();

            entity.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_Sales_NonNegativeAmounts",
                    """
            "Subtotal" >= 0
            AND "DiscountAmount" >= 0
            AND "LineDiscountAmount" >= 0
            AND "OrderDiscountAmount" >= 0
            AND "TransportAmount" >= 0
            AND "TaxAmount" >= 0
            AND "TotalAmount" >= 0
            AND "PaidAmount" >= 0
            AND "DueAmount" >= 0
            """);

                t.HasCheckConstraint(
                    "CK_Sales_TaxRate",
                    """
            "SalesTaxRate" >= 0
            AND "SalesTaxRate" <= 100
            """);
            });
        });

        modelBuilder.Entity<StockBalance>(entity =>
        {
            entity.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_StockBalances_ValidQuantities",
                    """
            "QuantityOnHand" >= 0
            AND "QuantityReserved" >= 0
            AND "QuantityReserved" <= "QuantityOnHand"
            """);
            });
        });

        modelBuilder.Entity<CustomerPaymentOperation>(entity =>
        {
            entity.ToTable("CustomerPaymentOperations");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.PaymentRequestId)
                .IsUnique();

            entity.Property(x => x.Amount)
                .HasPrecision(18, 2);

            entity.Property(x => x.RequestFingerprint)
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(x => x.ResultJson)
                .IsRequired();
        });
    }
}