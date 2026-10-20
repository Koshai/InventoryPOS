using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace InventoryPOS.Desktop.Views;

public sealed partial class ReportsPage : Page
{
    private bool _loaded;

    public ReportsViewModel ViewModel { get; }

    public ReportsPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<ReportsViewModel>();
        DataContext = ViewModel;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (_loaded)
            return;
        _loaded = true;
        await ViewModel.LoadLookupsAsync();
    }

    private async void RunSales_Click(object sender, RoutedEventArgs e) =>
        await ViewModel.RunSalesSummaryAsync();

    private async void RunInventory_Click(object sender, RoutedEventArgs e) =>
        await ViewModel.RunInventoryAsync();

    private async void ExportSalesCsv_Click(object sender, RoutedEventArgs e)
    {
        var window = App.Services.GetRequiredService<MainWindow>();
        await ViewModel.ExportSalesSummaryCsvAsync(window);
    }
}
