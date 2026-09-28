using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace InventoryPOS.Desktop.Views;

public sealed partial class PurchaseReturnPage
    : Page
{
    public PurchaseReturnViewModel ViewModel
    {
        get;
    }

    public PurchaseReturnPage()
    {
        InitializeComponent();

        ViewModel =
            App.Services.GetRequiredService<
                PurchaseReturnViewModel>();
    }

    protected override async void OnNavigatedTo(
        NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is Guid goodsReceiptId &&
            goodsReceiptId != Guid.Empty)
        {
            await ViewModel.LoadAsync(
                goodsReceiptId);
        }
    }

    private void Back_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
            Frame.GoBack();
    }

    private async void CompleteReturn_Click(
        object sender,
        RoutedEventArgs e)
    {
        var result =
            await ViewModel.CompleteAsync();

        if (result is null)
            return;

        var dialog =
            new ContentDialog
            {
                XamlRoot =
                    XamlRoot,

                Title =
                    "Purchase return completed",

                Content =
                    $"Return {result.ReturnNumber} was completed successfully.\n\n" +
                    $"Quantity: {result.TotalQuantity:0.###}\n" +
                    $"Value: ৳{result.TotalAmount:N2}",

                CloseButtonText =
                    "OK"
            };

        await dialog.ShowAsync();

        if (Frame.CanGoBack)
            Frame.GoBack();
    }
}