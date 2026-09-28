using InventoryPOS.Application.DTOs.Purchasing;
using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System.Threading.Tasks;
using Windows.System;

namespace InventoryPOS.Desktop.Views;

public sealed partial class PurchaseReturnHistoryPage : Page
{
    public PurchaseReturnHistoryViewModel ViewModel { get; }

    public PurchaseReturnHistoryPage()
    {
        InitializeComponent();

        ViewModel =
            App.Services.GetRequiredService<
                PurchaseReturnHistoryViewModel>();
    }

    protected override async void OnNavigatedTo(
        NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        await ViewModel.LoadAsync();

        UpdateEmptyState();
    }

    private async void Refresh_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.LoadAsync();

        UpdateEmptyState();
    }

    private async void Search_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.SearchAsync();

        UpdateEmptyState();
    }

    private async void Clear_Click(
        object sender,
        RoutedEventArgs e)
    {
        ViewModel.ClearSearch();

        await ViewModel.SearchAsync();

        UpdateEmptyState();
    }

    private async void SearchBox_KeyDown(
        object sender,
        KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Enter)
            return;

        await ViewModel.SearchAsync();

        UpdateEmptyState();
    }

    private void ReturnsList_ItemClick(
        object sender,
        ItemClickEventArgs e)
    {
        if (e.ClickedItem
            is not PurchaseReturnHistoryDto item)
        {
            return;
        }

        Frame.Navigate(
            typeof(PurchaseReturnDetailsPage),
            item.Id);
    }

    private void UpdateEmptyState()
    {
        EmptyState.Visibility =
            ViewModel.Returns.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
    }

    private void BackToPurchaseOrders_Click(
    object sender,
    RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
        else
        {
            Frame.Navigate(typeof(PurchaseOrdersPage));
        }
    }
}