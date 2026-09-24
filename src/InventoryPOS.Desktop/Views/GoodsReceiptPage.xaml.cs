using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace InventoryPOS.Desktop.Views;

public sealed partial class GoodsReceiptPage : Page
{
    private Guid _purchaseOrderId;

    public GoodsReceiptViewModel ViewModel { get; }

    public GoodsReceiptPage()
    {
        InitializeComponent();

        ViewModel =
            App.Services
                .GetRequiredService<GoodsReceiptViewModel>();

        DataContext =
            ViewModel;
    }

    protected override async void OnNavigatedTo(
        NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is not Guid purchaseOrderId)
            return;

        _purchaseOrderId =
            purchaseOrderId;

        await ViewModel.LoadAsync(
            purchaseOrderId);

        ErrorInfoBar.IsOpen =
            !string.IsNullOrWhiteSpace(
                ViewModel.ErrorMessage);
    }

    private void ReceiveAllButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ViewModel.ReceiveAll();
    }

    private async void PostReceiptButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ErrorInfoBar.IsOpen = false;

        var receiptId =
            await ViewModel.SaveAsync();

        if (!receiptId.HasValue)
        {
            ErrorInfoBar.IsOpen =
                !string.IsNullOrWhiteSpace(
                    ViewModel.ErrorMessage);

            return;
        }

        Frame.Navigate(
            typeof(PurchaseOrderDetailsPage),
            _purchaseOrderId);
    }

    private void CancelButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
            Frame.GoBack();
    }
}