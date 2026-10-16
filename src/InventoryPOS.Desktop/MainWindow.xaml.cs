using InventoryPOS.Application.Common.Settings;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Desktop.Views;
using InventoryPOS.Domain.Enums;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Threading.Tasks;
using Windows.System;

namespace InventoryPOS.Desktop;

public sealed partial class MainWindow : Window
{
    private readonly AppSettings _appSettings;

    private readonly IDocumentLookupService
    _documentLookupService;

    public MainWindow(IOptions<AppSettings> appSettings,
        IRegionalSettingsService regionalSettings,
        IDocumentLookupService documentLookupService)
    {
        InitializeComponent();

        _appSettings =
            appSettings.Value;

        _documentLookupService =
            documentLookupService;

        ContentFrame.Navigate(
            typeof(ProductsPage));
    }

    private void MainNavigation_SelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer is not NavigationViewItem item)
            return;

        var tag = item.Tag?.ToString();

        switch (tag)
        {
            case "products":
                ContentFrame.Navigate(
                    typeof(ProductsPage));
                break;

            // We'll wire these as their pages are implemented.

            case "dashboard":
                break;

            case "pos":
                ContentFrame.Navigate(
                    typeof(POSPage));
                break;
            
            case "Sales":
                ContentFrame.Navigate(typeof(SalesPage));
                break;

            case "Quotations":
                ContentFrame.Navigate(
                    typeof(SalesQuoteHistoryPage));
                break;
            case "Warranties":
                ContentFrame.Navigate(
                    typeof(WarrantyLookupPage));
                break;
            case "WarrantyClaims":
                ContentFrame.Navigate(typeof(WarrantyClaimsPage));
                break;
            case "inventory":
                ContentFrame.Navigate(
                    typeof(InventoryPage));
                break;
            case "suppliers":
                ContentFrame.Navigate(
                    typeof(SuppliersPage));
                break;
            case "purchases":
                ContentFrame.Navigate(
                    typeof(PurchaseOrdersPage));
                break;
            case "customers":
                ContentFrame.Navigate(
                    typeof(CustomersPage));
                break;
            case "licensing":
                ContentFrame.Navigate(
                    typeof(LicenseActivationPage));
                break;
            case "Settings":
                ContentFrame.Navigate(typeof(SettingsPage));
                break;
        }
    }

    private async void GlobalScanBox_KeyDown(
    object sender,
    KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Enter)
            return;

        e.Handled = true;

        await ProcessScanAsync();
    }

    private async void OpenScan_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ProcessScanAsync();
    }

    private async Task ProcessScanAsync()
    {
        var payload =
            GlobalScanBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(payload))
            return;

        try
        {
            var result =
                await _documentLookupService
                    .ResolveAsync(payload);

            if (result is null)
            {
                await ShowScanErrorAsync(
                    "Document Not Found",
                    "The scanned QR code is not a valid " +
                    "InventoryPOS document.");

                return;
            }

            NavigateToDocument(
                result.DocumentType,
                result.DocumentId);

            GlobalScanBox.Text =
                string.Empty;

            GlobalScanBox.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        }
        catch (Exception ex)
        {
            await ShowScanErrorAsync(
                "Scan Failed",
                ex.Message);
        }
    }

    private void NavigateToDocument(
    DocumentType documentType,
    Guid documentId)
    {
        switch (documentType)
        {
            case DocumentType.SalesQuote:

                ContentFrame.Navigate(
                    typeof(SalesQuoteDetailsPage),
                    documentId);

                break;


            case DocumentType.PurchaseRequisition:

                ContentFrame.Navigate(
                    typeof(PurchaseRequisitionDetailsPage),
                    documentId);

                break;


            case DocumentType.WarrantyClaim:

                ContentFrame.Navigate(
                    typeof(WarrantyClaimsPage),
                    documentId);

                break;


            case DocumentType.SaleInvoice:

                ContentFrame.Navigate(
                    typeof(InvoicePage),
                    documentId);

                break;


            default:

                throw new NotSupportedException(
                    $"Document type '{documentType}' " +
                    "does not have a screen yet.");
        }
    }

    private async Task ShowScanErrorAsync(
    string title,
    string message)
    {
        var dialog =
            new ContentDialog
            {
                Title =
                    title,

                Content =
                    message,

                CloseButtonText =
                    "OK",

                XamlRoot =
                    ContentFrame.XamlRoot
            };

        await dialog.ShowAsync();
    }
}