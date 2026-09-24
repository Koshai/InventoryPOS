using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace InventoryPOS.Desktop.Views;

public sealed partial class GoodsReceiptDetailsPage : Page
{
    private Guid _goodsReceiptId;

    public GoodsReceiptDetailsViewModel ViewModel { get; }

    public GoodsReceiptDetailsPage()
    {
        InitializeComponent();

        ViewModel =
            App.Services
                .GetRequiredService<GoodsReceiptDetailsViewModel>();

        DataContext = ViewModel;
    }

    protected override async void OnNavigatedTo(
        NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is not Guid goodsReceiptId)
            return;

        _goodsReceiptId = goodsReceiptId;

        await ViewModel.LoadAsync(_goodsReceiptId);

        ErrorInfoBar.IsOpen =
            !string.IsNullOrWhiteSpace(
                ViewModel.ErrorMessage);
    }

    private void BackButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
            Frame.GoBack();
        else
            Frame.Navigate(typeof(PurchaseOrdersPage));
    }
}