using InventoryPOS.Application.DTOs.Sales;
using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace InventoryPOS.Desktop.Views;

public sealed partial class POSPage : Page
{
    public PosViewModel ViewModel { get; }

    public POSPage()
    {
        InitializeComponent();

        ViewModel =
            App.Services.GetRequiredService<PosViewModel>();

        DataContext =
            ViewModel;
    }


    protected override async void OnNavigatedTo(
        NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        await ViewModel.LoadAsync();

        if (e.Parameter is Guid quoteId &&
            quoteId != Guid.Empty)
        {
            await ViewModel.LoadQuoteAsync(quoteId);
        }

        ShowError();
    }


    // ---------------------------------------------------------
    // Product
    // ---------------------------------------------------------

    private void Product_ItemClick(
        object sender,
        ItemClickEventArgs e)
    {
        if (e.ClickedItem is not
            PosProductDto product)
        {
            return;
        }

        ViewModel.AddProduct(
            product);

        ShowError();
    }

    // ---------------------------------------------------------
    // Cart
    // ---------------------------------------------------------

    private void RemoveLineButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.Tag is not
                PosCartLineViewModel line)
        {
            return;
        }

        ViewModel.RemoveLine(
            line);

        ShowError();
    }

    private void Quantity_ValueChanged(
        NumberBox sender,
        NumberBoxValueChangedEventArgs args)
    {
        if (sender.DataContext is not
            PosCartLineViewModel line)
        {
            return;
        }

        ViewModel.ValidateQuantity(
            line);

        ShowError();
    }

    // ---------------------------------------------------------
    // Payments
    // ---------------------------------------------------------

    private void AddPaymentButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ViewModel.AddPayment();

        ShowError();
    }

    private void RemovePaymentButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.Tag is not
                PosPaymentLineViewModel payment)
        {
            return;
        }

        ViewModel.RemovePayment(
            payment);

        ShowError();
    }

    // ---------------------------------------------------------
    // Customer
    // ---------------------------------------------------------

    private void WalkInCustomerButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ViewModel.SelectWalkInCustomer();

        ShowError();
    }

    // ---------------------------------------------------------
    // Quote
    // ---------------------------------------------------------

    private async void CreateQuoteButton_Click(
    object sender,
    RoutedEventArgs e)
    {
        ErrorInfoBar.IsOpen =
            false;

        var validUntilPicker =
            new CalendarDatePicker
            {
                Header = "Valid Until",
                PlaceholderText = "Optional"
            };

        var notesBox =
            new TextBox
            {
                Header = "Notes",
                PlaceholderText =
                    "Optional notes for the customer",
                AcceptsReturn = true,
                TextWrapping =
                    TextWrapping.Wrap,
                MinHeight = 70
            };

        var termsBox =
            new TextBox
            {
                Header = "Terms",
                PlaceholderText =
                    "Optional quotation terms",
                AcceptsReturn = true,
                TextWrapping =
                    TextWrapping.Wrap,
                MinHeight = 70
            };

        var content =
            new StackPanel
            {
                Spacing = 12
            };

        content.Children.Add(
            validUntilPicker);

        content.Children.Add(
            notesBox);

        content.Children.Add(
            termsBox);

        var dialog =
            new ContentDialog
            {
                Title =
                    "Create Quotation",

                Content =
                    content,

                PrimaryButtonText =
                    "Create Quote",

                CloseButtonText =
                    "Cancel",

                DefaultButton =
                    ContentDialogButton.Primary,

                XamlRoot =
                    XamlRoot
            };

        var dialogResult =
            await dialog.ShowAsync();

        if (dialogResult !=
            ContentDialogResult.Primary)
        {
            return;
        }

        DateTime? validUntilDate = null;

        if (validUntilPicker.Date.HasValue)
        {
            var selectedDate =
                validUntilPicker.Date.Value;

            validUntilDate =
                new DateTime(
                    selectedDate.Year,
                    selectedDate.Month,
                    selectedDate.Day,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc);
        }

        var result =
            await ViewModel.CreateQuoteAsync(
                validUntilDate,
                notesBox.Text,
                termsBox.Text);

        if (result is null)
        {
            ShowError();
            return;
        }

        var successDialog =
            new ContentDialog
            {
                Title =
                    "Quotation Created",

                Content =
                    $"Quote: {result.QuoteNumber}\n" +
                    $"Items: {result.LineCount}\n" +
                    $"Quantity: {result.TotalQuantity:N3}\n" +
                    $"Total: ৳{result.TotalAmount:N2}",

                CloseButtonText =
                    "OK",

                XamlRoot =
                    XamlRoot
            };

        await successDialog.ShowAsync();
    }

    // ---------------------------------------------------------
    // Complete Sale
    // ---------------------------------------------------------

    private async void CompleteSaleButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ErrorInfoBar.IsOpen =
            false;

        var result =
            await ViewModel
                .CompleteSaleAsync();

        if (result is null)
        {
            ShowError();
            return;
        }

        string paymentStatus;

        if (result.DueAmount <= 0)
        {
            paymentStatus =
                "Fully Paid";
        }
        else if (result.PaidAmount <= 0)
        {
            paymentStatus =
                "Credit";
        }
        else
        {
            paymentStatus =
                "Partially Paid";
        }

        var dialog =
            new ContentDialog
            {
                Title =
                    "Sale Completed",

                Content =
                    $"Invoice: {result.InvoiceNumber}\n" +
                    $"Total: ৳{result.TotalAmount:N2}\n" +
                    $"Paid: ৳{result.PaidAmount:N2}\n" +
                    $"Due: ৳{result.DueAmount:N2}\n" +
                    $"Status: {paymentStatus}",

                CloseButtonText =
                    "OK",

                XamlRoot =
                    XamlRoot
            };

        await dialog.ShowAsync();
    }

    // ---------------------------------------------------------
    // Error
    // ---------------------------------------------------------

    private void ShowError()
    {
        ErrorInfoBar.IsOpen =
            !string.IsNullOrWhiteSpace(
                ViewModel.ErrorMessage);
    }
}