
using InventoryPOS.Application.DTOs.Warranty;
using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using Windows.System;
using InventoryPOS.Application.Interfaces;
using System.Globalization;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace InventoryPOS.Desktop.Views;

public sealed partial class WarrantyLookupPage : Page
{
    public WarrantyLookupViewModel ViewModel { get; }

    public WarrantyLookupPage()
    {
        // Resolve before InitializeComponent so the ViewModel
        // is available if we later introduce x:Bind.
        ViewModel = App.Services
            .GetRequiredService<WarrantyLookupViewModel>();

        InitializeComponent();
        DataContext = ViewModel;
    }

    private async void Search_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.SearchAsync();
    }

    private async void InvoiceSearchBox_KeyDown(
        object sender,
        KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Enter)
            return;

        e.Handled = true;
        await ViewModel.SearchAsync();
    }

    protected override async void OnNavigatedTo(
        Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        await ViewModel.LoadRecentInvoicesAsync();
    }

    private async void SearchInvoices_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.SearchInvoicesAsync();
    }

    private async void InvoiceBrowserSearchBox_KeyDown(
        object sender,
        KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Enter)
            return;

        e.Handled = true;
        await ViewModel.SearchInvoicesAsync();
    }

    private async void InvoiceResults_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (InvoiceResultsList.SelectedItem
            is not WarrantyInvoiceSearchDto invoice)
        {
            return;
        }

        await ViewModel.SelectInvoiceAsync(invoice.SaleId);
    }

    private async void PreviousPage_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.PreviousPageAsync();
    }

    private async void NextPage_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.NextPageAsync();
    }


    private async void RegisterClaim_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.DataContext is not WarrantyLookupRowViewModel item ||
            !item.CanRegister ||
            ViewModel.SelectedSaleId is not Guid saleId)
        {
            return;
        }

        var quantityBox = new TextBox
        {
            Header = "Claim quantity",
            Text = "1",
            Description = $"Purchased quantity: {item.QuantityPurchased:0.###}"
        };

        var serialBox = new TextBox
        {
            Header = "Serial number (optional)",
            PlaceholderText = "Enter the product serial number"
        };

        var defectBox = new TextBox
        {
            Header = "Reported defect",
            PlaceholderText = "Describe the problem",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 90
        };

        var complaintBox = new TextBox
        {
            Header = "Customer complaint (optional)",
            PlaceholderText = "Additional information from the customer",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 80
        };

        var errorText = new TextBlock
        {
            Foreground = new SolidColorBrush(Colors.IndianRed),
            TextWrapping = TextWrapping.Wrap,
            Visibility = Visibility.Collapsed
        };

        var fields = new StackPanel { Spacing = 12 };
        fields.Children.Add(new TextBlock
        {
            Text = $"{item.ProductName} ({item.SKU})",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });
        fields.Children.Add(quantityBox);
        fields.Children.Add(serialBox);
        fields.Children.Add(defectBox);
        fields.Children.Add(complaintBox);
        fields.Children.Add(errorText);

        var dialog = new ContentDialog
        {
            XamlRoot = button.XamlRoot,
            Title = "Register Warranty Claim",
            Content = new ScrollViewer
            {
                Content = fields,
                MaxHeight = 460
            },
            PrimaryButtonText = "Submit Claim",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary
        };

        RegisterWarrantyClaimResultDto? registeredClaim = null;

        dialog.PrimaryButtonClick += async (s, args) =>
        {
            var deferral = args.GetDeferral();

            try
            {
                errorText.Visibility = Visibility.Collapsed;

                if (!decimal.TryParse(
                        quantityBox.Text,
                        NumberStyles.Number,
                        CultureInfo.CurrentCulture,
                        out var quantity) &&
                    !decimal.TryParse(
                        quantityBox.Text,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out quantity))
                {
                    throw new InvalidOperationException(
                        "Enter a valid claim quantity.");
                }

                if (quantity <= 0 ||
                    quantity > item.QuantityPurchased)
                {
                    throw new InvalidOperationException(
                        $"Quantity must be between 0 and " +
                        $"{item.QuantityPurchased:0.###}.");
                }

                if (string.IsNullOrWhiteSpace(defectBox.Text))
                {
                    throw new InvalidOperationException(
                        "Please describe the reported defect.");
                }

                var service = App.Services
                    .GetRequiredService<IWarrantyClaimService>();

                registeredClaim = await service.RegisterAsync(
                    new RegisterWarrantyClaimRequest
                    {
                        SaleId = saleId,
                        CustomerComplaint = complaintBox.Text,
                        Items =
                        {
                        new RegisterWarrantyClaimItemRequest
                        {
                            SaleLineId = item.SaleLineId,
                            Quantity = quantity,
                            SerialNumber = serialBox.Text,
                            ReportedDefect = defectBox.Text
                        }
                        }
                    });
            }
            catch (Exception ex)
            {
                args.Cancel = true;
                errorText.Text = ex.Message;
                errorText.Visibility = Visibility.Visible;
            }
            finally
            {
                deferral.Complete();
            }
        };

        await dialog.ShowAsync();

        if (registeredClaim is null)
            return;

        await ViewModel.SelectInvoiceAsync(saleId);

        var confirmation = new ContentDialog
        {
            XamlRoot = button.XamlRoot,
            Title = "Warranty Claim Submitted",
            Content =
                $"Claim number: {registeredClaim.ClaimNumber}\n\n" +
                $"Product: {item.ProductName}\n" +
                $"Quantity: {registeredClaim.TotalQuantity:0.###}\n\n" +
                "The claim is now awaiting inspection.",
            CloseButtonText = "OK"
        };

        await confirmation.ShowAsync();
    }

}

