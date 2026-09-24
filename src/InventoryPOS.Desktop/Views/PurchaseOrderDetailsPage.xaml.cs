using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using InventoryPOS.Application.DTOs.Purchasing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace InventoryPOS.Desktop.Views;

public sealed partial class PurchaseOrderDetailsPage : Page
{
    private Guid _purchaseOrderId;

    public PurchaseOrderDetailsViewModel ViewModel { get; }

    public PurchaseOrderDetailsPage()
    {
        InitializeComponent();

        ViewModel =
            App.Services
                .GetRequiredService<PurchaseOrderDetailsViewModel>();

        DataContext = ViewModel;
    }

    protected override async void OnNavigatedTo(
        NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is not Guid purchaseOrderId)
            return;

        _purchaseOrderId = purchaseOrderId;

        await ViewModel.LoadAsync(
            _purchaseOrderId);

        ErrorInfoBar.IsOpen =
            !string.IsNullOrWhiteSpace(
                ViewModel.ErrorMessage);
    }

    private void BackButton_Click(
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
                typeof(PurchaseOrdersPage));
        }
    }

    private void ReceiveGoodsButton_Click(
    object sender,
    RoutedEventArgs e)
    {
        Frame.Navigate(
            typeof(GoodsReceiptPage),
            _purchaseOrderId);
    }

    private void Receipt_ItemClick(
    object sender,
    ItemClickEventArgs e)
    {
        if (e.ClickedItem is not GoodsReceiptListItemDto receipt)
            return;

        Frame.Navigate(
            typeof(GoodsReceiptDetailsPage),
            receipt.Id);
    }
}