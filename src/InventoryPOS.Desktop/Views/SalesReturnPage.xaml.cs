using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace InventoryPOS.Desktop.Views;

public sealed partial class SalesReturnPage :
    Page
{
    public SalesReturnViewModel ViewModel { get; }

    private Guid _saleId;

    public SalesReturnPage()
    {
        InitializeComponent();

        ViewModel =
            App.Services.GetRequiredService<
                SalesReturnViewModel>();

        DataContext =
            ViewModel;
    }

    protected override async void OnNavigatedTo(
        NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is Guid saleId &&
            saleId != Guid.Empty)
        {
            _saleId =
                saleId;

            await ViewModel.LoadAsync(
                saleId);
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

        if (_saleId != Guid.Empty)
        {
            Frame.Navigate(
                typeof(InvoicePage),
                _saleId);

            return;
        }

        Frame.Navigate(
            typeof(SalesPage));
    }

    private async void CompleteReturnButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            CompleteReturnButton.IsEnabled =
                false;

            var result =
                await ViewModel.SubmitAsync();

            if (result is null)
                return;

            var dialog =
                new ContentDialog
                {
                    Title =
                        "Return Completed",

                    Content =
                        $"Return {result.ReturnNumber} was completed successfully.\n\n" +
                        $"Return amount: ৳{result.ReturnAmount:N2}\n" +
                        $"Applied to balance: ৳{result.CreditAdjustmentAmount:N2}\n" +
                        $"Refunded: ৳{result.RefundedAmount:N2}\n" +
                        $"Remaining due: ৳{result.RemainingSaleDue:N2}",

                    PrimaryButtonText =
                        "View Invoice",

                    CloseButtonText =
                        "Stay Here",

                    XamlRoot =
                        XamlRoot
                };

            var dialogResult =
                await dialog.ShowAsync();

            if (dialogResult ==
                ContentDialogResult.Primary)
            {
                /*
                 * Remove the return page from the back stack by
                 * navigating back to the existing invoice where
                 * possible.
                 */
                if (Frame.CanGoBack)
                {
                    Frame.GoBack();
                }
                else
                {
                    Frame.Navigate(
                        typeof(InvoicePage),
                        _saleId);
                }
            }
        }
        finally
        {
            CompleteReturnButton.IsEnabled =
                ViewModel.CanSubmit;
        }
    }
}