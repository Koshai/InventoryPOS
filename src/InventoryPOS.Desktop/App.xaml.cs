using InventoryPOS.Application;
using InventoryPOS.Application.Common.Settings;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Desktop.Services;
using InventoryPOS.Desktop.ViewModels;
using InventoryPOS.Infrastructure;
using InventoryPOS.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using QuestPDF.Drawing;
using QuestPDF.Infrastructure;
using System;
using System.IO;

namespace InventoryPOS.Desktop;

public partial class App : Microsoft.UI.Xaml.Application
{
    private readonly IHost _host;

    public static IServiceProvider Services =>
        ((App)Current)._host.Services;

    public App()
    {
        InitializeComponent();

        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(AppContext.BaseDirectory);

                config.AddJsonFile(
                    "appsettings.json",
                    optional: false,
                    reloadOnChange: false);
            })
            .ConfigureServices((context, services) =>
            {
                services.Configure<AppSettings>(
                    context.Configuration.GetSection(
                        AppSettings.SectionName));

                services.AddApplication();

                services.AddInfrastructure(
                    context.Configuration);

                services.AddSingleton<MainWindow>();

                services.AddSingleton<IRegionalSettingsService, RegionalSettingsService>();
                services.AddSingleton<IPdfPrintService, PdfPrintService>();
                services.AddSingleton<IBusinessLogoService, BusinessLogoService>();
                services.AddSingleton<IBusinessLogoProvider, BusinessLogoProvider>();

                services.AddTransient<ProductsViewModel>();
                services.AddTransient<AddProductViewModel>();
                services.AddTransient<ProductDetailsViewModel>();
                services.AddTransient<EditProductViewModel>();
                services.AddTransient<InventoryViewModel>();
                services.AddTransient<SetOpeningStockViewModel>();
                services.AddTransient<StockAdjustmentViewModel>();
                services.AddTransient<InventoryLedgerViewModel>();
                services.AddTransient<StockAdjustmentDetailsViewModel>();
                services.AddTransient<SuppliersViewModel>();
                services.AddTransient<SupplierFormViewModel>();
                services.AddTransient<SupplierDetailsViewModel>();
                services.AddTransient<PurchaseOrdersViewModel>();
                services.AddTransient<CreatePurchaseOrderViewModel>();
                services.AddTransient<PurchaseOrderDetailsViewModel>();
                services.AddTransient<GoodsReceiptViewModel>();
                services.AddTransient<GoodsReceiptDetailsViewModel>();
                services.AddTransient<CustomersViewModel>();
                services.AddTransient<CustomerFormViewModel>();
                services.AddTransient<CustomerDetailsViewModel>();
                services.AddTransient<PosViewModel>();
                services.AddTransient<SalesViewModel>();
                services.AddTransient<InvoiceViewModel>();
                services.AddTransient<BusinessProfileViewModel>();
                services.AddTransient<CustomerAccountViewModel>();
            })
            .Build();
    }

    protected override async void OnLaunched(
        LaunchActivatedEventArgs args)
    {
        ConfigureQuestPdf();

        await _host.StartAsync();

        await DatabaseInitializer.InitializeAsync(
            _host.Services);

        var window =
            _host.Services.GetRequiredService<MainWindow>();

        window.Activate();
    }

    private static void ConfigureQuestPdf()
    {
        QuestPDF.Settings.License =
            LicenseType.Community;

        var fontFolder =
            Path.Combine(
                AppContext.BaseDirectory,
                "Assets",
                "Fonts");

        var regularFontPath =
            Path.Combine(
                fontFolder,
                "NotoSansBengali-Regular.ttf");

        var semiBoldFontPath =
            Path.Combine(
                fontFolder,
                "NotoSansBengali-SemiBold.ttf");

        if (!File.Exists(regularFontPath))
        {
            throw new FileNotFoundException(
                "Noto Sans Bengali Regular font was not found.",
                regularFontPath);
        }

        if (!File.Exists(semiBoldFontPath))
        {
            throw new FileNotFoundException(
                "Noto Sans Bengali SemiBold font was not found.",
                semiBoldFontPath);
        }

        using (var regularFont =
               File.OpenRead(regularFontPath))
        {
            FontManager.RegisterFontWithCustomName(
                "Noto Sans Bengali",
                regularFont);
        }

        using (var semiBoldFont =
               File.OpenRead(semiBoldFontPath))
        {
            FontManager.RegisterFontWithCustomName(
                "Noto Sans Bengali SemiBold",
                semiBoldFont);
        }
    }
}