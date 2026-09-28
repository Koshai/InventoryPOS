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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(InventoryPosDbContext).Assembly);
    }
}