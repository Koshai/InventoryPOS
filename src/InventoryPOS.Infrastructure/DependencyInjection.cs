using InventoryPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using InventoryPOS.Infrastructure.Persistence.Seed;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Infrastructure.Services;

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

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "InventoryPOS database connection string is missing.");
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
        services.AddSingleton<IInvoicePdfService, InvoicePdfService>();
        services.AddSingleton<IPurchaseRequisitionPdfService, PurchaseRequisitionPdfService>();
        services.AddSingleton<ISalesQuotePdfService, SalesQuotePdfService>();
        services.AddSingleton<IWarrantyClaimPdfService, WarrantyClaimPdfService>();

        return services;
    }
}