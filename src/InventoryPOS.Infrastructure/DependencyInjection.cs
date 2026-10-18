using InventoryPOS.Application.Interfaces;
using InventoryPOS.Application.Pricing;
using InventoryPOS.Infrastructure.Persistence;
using InventoryPOS.Infrastructure.Persistence.Seed;
using InventoryPOS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryPOS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString(
                "InventoryPosDatabase");

        var isConfigured = !string.IsNullOrWhiteSpace(connectionString);

        services.AddSingleton(new DatabaseConnectionStatus(isConfigured));

        // When the installer has not written a local connection string yet,
        // register a non-functional placeholder so the UI can still start
        // and show a setup message instead of crashing during host build.
        if (!isConfigured)
        {
            connectionString =
                "Host=127.0.0.1;Port=1;Database=inventorypos_unconfigured;" +
                "Username=inventorypos;Password=unconfigured";
        }

        services.AddDbContext<InventoryPosDbContext>(
            options =>
                options.UseNpgsql(connectionString));
        services.AddScoped<DatabaseSeeder>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICatalogLookupService, CatalogLookupService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IProductImageService,ProductImageService>();
        services.AddScoped<IStockAdjustmentService,StockAdjustmentService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        services.AddScoped<IGoodsReceiptService, GoodsReceiptService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ISaleService, SaleService>();
        services.AddScoped<IBusinessProfileService, BusinessProfileService>();
        services.AddScoped<ICustomerAccountService, CustomerAccountService>();
        services.AddScoped<ISalesReturnService, SalesReturnService>();
        services.AddScoped<ISaleFinancialService, SaleFinancialService>();
        services.AddScoped<IPurchaseReturnService, PurchaseReturnService>();
        services.AddScoped<IPurchaseRequisitionService, PurchaseRequisitionService>();
        services.AddScoped<ISalesQuoteService, SalesQuoteService>();
        services.AddScoped<IWarrantyLookupService, WarrantyLookupService>();
        services.AddScoped<IWarrantyClaimService, WarrantyClaimService>();
        services.AddScoped<IWarrantyClaimQueryService, WarrantyClaimQueryService>();
        services.AddScoped<IWarrantyClaimManagementService, WarrantyClaimManagementService>();
        services.AddScoped<IWarrantyReplacementService, WarrantyReplacementService>();
        services.AddScoped<IDocumentIdentifierService, DocumentIdentifierService>();
        services.AddScoped<IDocumentLookupService, DocumentLookupService>();
        services.AddScoped<IDocumentQrService, DocumentQrService>();
        services.AddScoped<IProductBarcodeService, ProductBarcodeService>();
        services.AddScoped<IProductBarcodeRenderService, ProductBarcodeRenderService>();
        services.AddScoped<IProductBarcodeLabelService, ProductBarcodeLabelService>();
        services.AddSingleton<IInvoicePdfService, InvoicePdfService>();
        services.AddSingleton<IPurchaseRequisitionPdfService, PurchaseRequisitionPdfService>();
        services.AddSingleton<ISalesQuotePdfService, SalesQuotePdfService>();
        services.AddSingleton<IWarrantyClaimPdfService, WarrantyClaimPdfService>();
        services.AddSingleton<IQrCodeService, QrCodeService>();
        services.AddSingleton<IPricingCalculator, PricingCalculator>();

        return services;
    }
}