using InventoryPOS.Application.DTOs.Suppliers;
using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace InventoryPOS.Desktop.Views;

public sealed partial class SuppliersPage : Page
{
    public SuppliersViewModel ViewModel { get; }

    public SuppliersPage()
    {
        InitializeComponent();

        ViewModel =
            App.Services
                .GetRequiredService<SuppliersViewModel>();

        DataContext = ViewModel;
    }

    protected override async void OnNavigatedTo(
        NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        await ViewModel.LoadAsync();
    }

    private void AddSupplierButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Frame.Navigate(
            typeof(SupplierFormPage));
    }

    private void SuppliersListView_ItemClick(
        object sender,
        ItemClickEventArgs e)
    {
        if (e.ClickedItem is not SupplierListItemDto supplier)
            return;

        Frame.Navigate(
            typeof(SupplierDetailsPage),
            supplier.Id);
    }
}