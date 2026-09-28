using InventoryPOS.Application.DTOs.Purchasing;
using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace InventoryPOS.Desktop.Views;

public sealed partial class PurchaseOrdersPage : Page
{
    public PurchaseOrdersViewModel ViewModel { get; }

    public PurchaseOrdersPage()
    {
        InitializeComponent();

        ViewModel =
            App.Services
                .GetRequiredService<PurchaseOrdersViewModel>();

        DataContext = ViewModel;
    }

    protected override async void OnNavigatedTo(
        NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        await ViewModel.LoadAsync();
    }

    private void NewPurchaseOrderButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Frame.Navigate(
            typeof(CreatePurchaseOrderPage));
    }

    private void PurchaseOrdersListView_ItemClick(
        object sender,
        ItemClickEventArgs e)
    {
        if (e.ClickedItem is not PurchaseOrderListItemDto order)
            return;

        Frame.Navigate(
            typeof(PurchaseOrderDetailsPage),
            order.Id);
    }

    private void PurchaseReturnsButton_Click(
    object sender,
    RoutedEventArgs e)
    {
        Frame.Navigate(
            typeof(PurchaseReturnHistoryPage));
    }

    private void RequisitionsButton_Click(
    object sender,
    RoutedEventArgs e)
    {
        Frame.Navigate(
            typeof(PurchaseRequisitionHistoryPage));
    }
}