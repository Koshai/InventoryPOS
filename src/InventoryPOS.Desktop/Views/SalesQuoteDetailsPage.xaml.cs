using InventoryPOS.Application.Interfaces;
using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.Views;

public sealed partial class SalesQuoteDetailsPage :
    Page
{
    private readonly ISalesQuotePdfService
        _salesQuotePdfService;

    private readonly IPdfPrintService
        _pdfPrintService;

    public SalesQuoteDetailsViewModel
        ViewModel
    { get; }

    public SalesQuoteDetailsPage()
    {
        ViewModel =
            App.Services.GetRequiredService<
                SalesQuoteDetailsViewModel>();

        _salesQuotePdfService =
            App.Services.GetRequiredService<
                ISalesQuotePdfService>();

        _pdfPrintService =
            App.Services.GetRequiredService<
                IPdfPrintService>();

        InitializeComponent();

        DataContext =
            ViewModel;
    }

    protected override async void OnNavigatedTo(
        NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is not Guid quoteId ||
            quoteId == Guid.Empty)
        {
            return;
        }

        await ViewModel.LoadAsync(
            quoteId);

        ShowError();
    }

    // =========================================================
    // EXPORT PDF
    // =========================================================

    private async void ExportPdfButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (ViewModel.Quote is null)
            return;

        try
        {
            var picker =
                new Windows.Storage.Pickers
                    .FileSavePicker();

            var hwnd =
                WinRT.Interop.WindowNative
                    .GetWindowHandle(
                        App.Services
                            .GetRequiredService<
                                MainWindow>());

            WinRT.Interop.InitializeWithWindow
                .Initialize(
                    picker,
                    hwnd);

            picker.SuggestedStartLocation =
                Windows.Storage.Pickers
                    .PickerLocationId
                    .DocumentsLibrary;

            picker.FileTypeChoices.Add(
                "PDF Document",
                new List<string>
                {
                    ".pdf"
                });

            picker.SuggestedFileName =
                $"Quotation-" +
                $"{ViewModel.Quote.QuoteNumber}";

            var file =
                await picker.PickSaveFileAsync();

            if (file is null)
                return;

            var pdf =
                await _salesQuotePdfService
                    .GenerateQuotationPdfAsync(
                        ViewModel.Quote);

            await Windows.Storage.FileIO
                .WriteBytesAsync(
                    file,
                    pdf);
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(
                "PDF Export Failed",
                ex.Message);
        }
    }

    // =========================================================
    // PRINT
    // =========================================================

    private async void PrintButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (ViewModel.Quote is null)
            return;

        try
        {
            var pdf =
                await _salesQuotePdfService
                    .GenerateQuotationPdfAsync(
                        ViewModel.Quote);

            await _pdfPrintService.PrintAsync(
                pdf,
                $"Quotation-" +
                $"{ViewModel.Quote.QuoteNumber}");
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(
                "Print Failed",
                ex.Message);
        }
    }

    // =========================================================
    // ERROR
    // =========================================================

    private void ShowError()
    {
        ErrorInfoBar.IsOpen =
            !string.IsNullOrWhiteSpace(
                ViewModel.ErrorMessage);
    }

    private async Task ShowErrorDialogAsync(
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
                    XamlRoot
            };

        await dialog.ShowAsync();
    }

    private async void IssueButton_Click(
    object sender,
    RoutedEventArgs e)
    {
        if (!await ConfirmAsync(
                "Issue Quotation",
                "Issue this quotation to the customer?",
                "Issue"))
        {
            return;
        }

        await ViewModel.IssueAsync();

        ShowError();
    }


    private async void AcceptButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!await ConfirmAsync(
                "Accept Quotation",
                "Mark this quotation as accepted by the customer?",
                "Accept"))
        {
            return;
        }

        await ViewModel.AcceptAsync();

        ShowError();
    }


    private async void CancelButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!await ConfirmAsync(
                "Cancel Quotation",
                "Cancel this quotation? This action cannot be reversed.",
                "Cancel Quote"))
        {
            return;
        }

        await ViewModel.CancelAsync();

        ShowError();
    }


    private async void ExpireButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!await ConfirmAsync(
                "Expire Quotation",
                "Mark this quotation as expired?",
                "Mark Expired"))
        {
            return;
        }

        await ViewModel.ExpireAsync();

        ShowError();
    }

    private async Task<bool> ConfirmAsync(
    string title,
    string message,
    string confirmText)
    {
        var dialog =
            new ContentDialog
            {
                Title =
                    title,

                Content =
                    message,

                PrimaryButtonText =
                    confirmText,

                CloseButtonText =
                    "Back",

                DefaultButton =
                    ContentDialogButton.Primary,

                XamlRoot =
                    XamlRoot
            };

        var result =
            await dialog.ShowAsync();

        return result ==
            ContentDialogResult.Primary;
    }


    private void LoadIntoPosButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (ViewModel.Quote is null ||
            !ViewModel.CanLoadIntoPos)
        {
            return;
        }

        Frame.Navigate(
            typeof(POSPage),
            ViewModel.Quote.Id);
    }

}