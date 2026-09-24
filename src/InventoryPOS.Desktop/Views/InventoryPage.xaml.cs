using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace InventoryPOS.Desktop.Views;

public sealed partial class InventoryPage : Page
{
    public InventoryViewModel ViewModel { get; }

    public InventoryPage()
    {
        InitializeComponent();

        ViewModel =
            App.Services.GetRequiredService<InventoryViewModel>();

        DataContext = ViewModel;
    }

    protected override async void OnNavigatedTo(
        NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        await ViewModel.LoadAsync();
    }

    private void SetOpeningStockButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Frame.Navigate(typeof(SetOpeningStockPage));
    }
    private void AdjustStockButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Frame.Navigate(
            typeof(StockAdjustmentPage));
    }
    private void InventoryLedgerButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Frame.Navigate(
            typeof(InventoryLedgerPage));
    }
}