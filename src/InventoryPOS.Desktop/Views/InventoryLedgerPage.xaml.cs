using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using InventoryPOS.Application.DTOs.Inventory;

namespace InventoryPOS.Desktop.Views;

public sealed partial class InventoryLedgerPage : Page
{
    public InventoryLedgerViewModel ViewModel { get; }

    public InventoryLedgerPage()
    {
        InitializeComponent();

        ViewModel =
            App.Services
                .GetRequiredService<InventoryLedgerViewModel>();

        DataContext =
            ViewModel;
    }

    protected override async void OnNavigatedTo(
        NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        await ViewModel.LoadAsync();
    }

    private void BackButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }

    private void LedgerListView_ItemClick(
        object sender,
        ItemClickEventArgs e)
    {
        if (e.ClickedItem is not InventoryLedgerItemDto item)
            return;

        if (item.ReferenceType != "StockAdjustment")
            return;

        if (!item.ReferenceId.HasValue)
            return;

        Frame.Navigate(
            typeof(StockAdjustmentDetailsPage),
            item.ReferenceId.Value);
    }
}