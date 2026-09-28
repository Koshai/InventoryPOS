using InventoryPOS.Application.DTOs.Purchasing;
using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.System;

namespace InventoryPOS.Desktop.Views;

public sealed partial class PurchaseRequisitionHistoryPage : Page
{
    public PurchaseRequisitionHistoryViewModel ViewModel { get; }

    public PurchaseRequisitionHistoryPage()
    {
        InitializeComponent();

        ViewModel =
            App.Services.GetRequiredService<
                PurchaseRequisitionHistoryViewModel>();
    }

    protected override async void OnNavigatedTo(
        NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        await ViewModel.LoadAsync();
    }

    private void Back_Click(
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

    private void NewRequisition_Click(
        object sender,
        RoutedEventArgs e)
    {
        Frame.Navigate(
            typeof(CreatePurchaseRequisitionPage));
    }

    private async void Refresh_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.LoadAsync();
    }

    private async void Search_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.SearchAsync();
    }

    private async void Clear_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.ClearSearchAsync();
    }

    private async void SearchBox_KeyDown(
        object sender,
        KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Enter)
            return;

        await ViewModel.SearchAsync();
    }

    private void Requisition_ItemClick(
    object sender,
    ItemClickEventArgs e)
    {
        if (e.ClickedItem is not
            PurchaseRequisitionHistoryDto item)
        {
            return;
        }

        Frame.Navigate(
            typeof(PurchaseRequisitionDetailsPage),
            item.Id);
    }
}