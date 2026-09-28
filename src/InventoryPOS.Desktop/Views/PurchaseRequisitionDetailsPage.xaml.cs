using InventoryPOS.Application.Interfaces;
using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace InventoryPOS.Desktop.Views;

public sealed partial class PurchaseRequisitionDetailsPage : Page
{
    public PurchaseRequisitionDetailsViewModel ViewModel { get; }

    private readonly IPdfPrintService _pdfPrintService;

    public PurchaseRequisitionDetailsPage()
    {
        ViewModel =
            App.Services.GetRequiredService<
                PurchaseRequisitionDetailsViewModel>();

        _pdfPrintService =
            App.Services.GetRequiredService<
                IPdfPrintService>();

        InitializeComponent();
    }

    protected override async void OnNavigatedTo(
        NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is Guid requisitionId)
        {
            await ViewModel.LoadAsync(
                requisitionId);

            return;
        }

        // Useful if the ID is ever passed as a string.
        if (e.Parameter is string value &&
            Guid.TryParse(value, out var parsedId))
        {
            await ViewModel.LoadAsync(
                parsedId);
        }
    }

    private void Back_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
        else
        {
            Frame.Navigate(
                typeof(PurchaseRequisitionHistoryPage));
        }
    }

    private async void ExportPdf_Click(
    object sender,
    RoutedEventArgs e)
    {
        if (ViewModel.Requisition is null)
            return;

        try
        {
            var picker =
                new Windows.Storage.Pickers.FileSavePicker();

            var hwnd =
                WinRT.Interop.WindowNative.GetWindowHandle(
                    App.Services.GetRequiredService<MainWindow>());

            WinRT.Interop.InitializeWithWindow.Initialize(
                picker,
                hwnd);

            picker.SuggestedStartLocation =
                Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;

            picker.FileTypeChoices.Add(
                "PDF Document",
                new List<string>
                {
                ".pdf"
                });

            picker.SuggestedFileName =
                $"RFQ-{ViewModel.Requisition.RequisitionNumber}";

            var file =
                await picker.PickSaveFileAsync();

            if (file is null)
                return;

            var pdf =
                await ViewModel.GeneratePdfAsync();

            if (pdf is null ||
                pdf.Length == 0)
            {
                return;
            }

            await Windows.Storage.FileIO.WriteBytesAsync(
                file,
                pdf);
        }
        catch (Exception ex)
        {
            var dialog =
                new ContentDialog
                {
                    Title = "RFQ PDF Export Failed",
                    Content = ex.Message,
                    CloseButtonText = "OK",
                    XamlRoot = XamlRoot
                };

            await dialog.ShowAsync();
        }
    }

    private async void PrintPdf_Click(
    object sender,
    RoutedEventArgs e)
    {
        if (ViewModel.Requisition is null)
            return;

        try
        {
            if (sender is Button button)
                button.IsEnabled = false;

            var pdf =
                await ViewModel.GeneratePdfAsync();

            if (pdf is null ||
                pdf.Length == 0)
            {
                return;
            }

            await _pdfPrintService.PrintAsync(
                pdf,
                $"RFQ-{ViewModel.Requisition.RequisitionNumber}");
        }
        catch (Exception ex)
        {
            var dialog =
                new ContentDialog
                {
                    Title = "Print RFQ Failed",
                    Content = ex.Message,
                    CloseButtonText = "OK",
                    XamlRoot = XamlRoot
                };

            await dialog.ShowAsync();
        }
        finally
        {
            if (sender is Button button)
                button.IsEnabled = true;
        }
    }

    private async void MarkAsSent_Click(
    object sender,
    RoutedEventArgs e)
    {
        if (!ViewModel.CanMarkAsSent)
            return;

        var dialog =
            new ContentDialog
            {
                Title = "Mark Requisition as Sent?",
                Content =
                    "This records that the requisition / RFQ " +
                    "has been sent to the supplier.",
                PrimaryButtonText = "Mark as Sent",
                CloseButtonText = "Cancel",
                DefaultButton =
                    ContentDialogButton.Primary,
                XamlRoot = XamlRoot
            };

        var result =
            await dialog.ShowAsync();

        if (result !=
            ContentDialogResult.Primary)
        {
            return;
        }

        await ViewModel.MarkAsSentAsync();
    }

    private async void Cancel_Click(
    object sender,
    RoutedEventArgs e)
    {
        if (!ViewModel.CanCancel)
            return;

        var dialog =
            new ContentDialog
            {
                Title = "Cancel Requisition?",
                Content =
                    "The requisition will be marked as cancelled. " +
                    "No inventory quantities will be changed.",
                PrimaryButtonText = "Cancel Requisition",
                CloseButtonText = "Keep Requisition",
                DefaultButton =
                    ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };

        var result =
            await dialog.ShowAsync();

        if (result !=
            ContentDialogResult.Primary)
        {
            return;
        }

        await ViewModel.CancelAsync();
    }

    private void ConvertToPurchaseOrder_Click(
    object sender,
    RoutedEventArgs e)
    {
        if (ViewModel.Requisition is null ||
            !ViewModel.CanConvertToPurchaseOrder)
        {
            return;
        }

        Frame.Navigate(
            typeof(CreatePurchaseOrderPage),
            ViewModel.Requisition.Id);
    }
}