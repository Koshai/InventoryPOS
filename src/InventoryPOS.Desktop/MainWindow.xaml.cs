using InventoryPOS.Application.Common.Settings;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Desktop.Views;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace InventoryPOS.Desktop;

public sealed partial class MainWindow : Window
{
    private readonly AppSettings _appSettings;

    public MainWindow(
        IOptions<AppSettings> appSettings, IRegionalSettingsService regionalSettings)
    {
        InitializeComponent();

        ContentFrame.Navigate(
            typeof(ProductsPage));
    }

    private void MainNavigation_SelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer is not NavigationViewItem item)
            return;

        var tag = item.Tag?.ToString();

        switch (tag)
        {
            case "products":
                ContentFrame.Navigate(
                    typeof(ProductsPage));
                break;

            // We'll wire these as their pages are implemented.

            case "dashboard":
                break;

            case "pos":
                ContentFrame.Navigate(
                    typeof(POSPage));
                break;
            
            case "Sales":
                ContentFrame.Navigate(typeof(SalesPage));
                break;

            case "inventory":
                ContentFrame.Navigate(
                    typeof(InventoryPage));
                break;
            case "suppliers":
                ContentFrame.Navigate(
                    typeof(SuppliersPage));
                break;
            case "purchases":
                ContentFrame.Navigate(
                    typeof(PurchaseOrdersPage));
                break;
            case "customers":
                ContentFrame.Navigate(
                    typeof(CustomersPage));
                break;
            case "Settings":
                ContentFrame.Navigate(typeof(SettingsPage));
                break;
        }
    }
}