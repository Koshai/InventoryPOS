using InventoryPOS.Application.Interfaces;
using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;

namespace InventoryPOS.Desktop.Views;

public sealed partial class InvoicePage : Page
{
    public InvoiceViewModel ViewModel { get; }

    private Guid _saleId;
    private readonly IInvoicePdfService _invoicePdfService;
    private readonly IPdfPrintService _pdfPrintService;

    public InvoicePage()
    {
        InitializeComponent();

        ViewModel =
            App.Services.GetRequiredService<InvoiceViewModel>();

        _invoicePdfService =
            App.Services.GetRequiredService<IInvoicePdfService>();

        _pdfPrintService =
            App.Services.GetRequiredService<IPdfPrintService>();

        DataContext = ViewModel;
    }

    protected override async void OnNavigatedTo(
        NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is Guid saleId &&
            saleId != Guid.Empty)
        {
            _saleId = saleId;

            await ViewModel.LoadAsync(
                saleId);

            return;
        }

        if (e.Parameter is string saleIdText &&
            Guid.TryParse(
                saleIdText,
                out var parsedSaleId))
        {
            _saleId = parsedSaleId;

            await ViewModel.LoadAsync(
                parsedSaleId);
        }
    }

    private void BackButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
            return;
        }

        Frame.Navigate(
            typeof(SalesPage));
    }

    private async void ExportPdfButton_Click(
    object sender,
    RoutedEventArgs e)
    {
        if (ViewModel.Sale is null)
            return;

        try
        {
            var picker =
                new Windows.Storage.Pickers.FileSavePicker();

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Services.GetRequiredService<MainWindow>());

            WinRT.Interop.InitializeWithWindow.Initialize(
                picker,
                hwnd);

            picker.SuggestedStartLocation =
                Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;

            picker.FileTypeChoices.Add(
                "PDF Document",
                new List<string> { ".pdf" });

            picker.SuggestedFileName =
                $"Invoice-{ViewModel.Sale.InvoiceNumber}";

            var file =
                await picker.PickSaveFileAsync();

            if (file is null)
                return;

            var pdf =
                await _invoicePdfService.GenerateInvoicePdfAsync(
                    ViewModel.Sale);

            await Windows.Storage.FileIO.WriteBytesAsync(
                file,
                pdf);
        }
        catch (Exception ex)
        {
            var dialog =
                new ContentDialog
                {
                    Title = "PDF Export Failed",
                    Content = ex.Message,
                    CloseButtonText = "OK",
                    XamlRoot = XamlRoot
                };

            await dialog.ShowAsync();
        }
    }

    private async void PrintButton_Click(
    object sender,
    RoutedEventArgs e)
    {
        if (ViewModel.Sale is null)
            return;

        try
        {
            PrintButton.IsEnabled = false;

            var pdf =
                await _invoicePdfService.GenerateInvoicePdfAsync(
                    ViewModel.Sale);

            await _pdfPrintService.PrintAsync(
                pdf,
                $"Invoice-{ViewModel.Sale.InvoiceNumber}");
        }
        catch (Exception ex)
        {
            var dialog =
                new ContentDialog
                {
                    Title = "Print Invoice Failed",
                    Content = ex.Message,
                    CloseButtonText = "OK",
                    XamlRoot = XamlRoot
                };

            await dialog.ShowAsync();
        }
        finally
        {
            PrintButton.IsEnabled = true;
        }
    }
}