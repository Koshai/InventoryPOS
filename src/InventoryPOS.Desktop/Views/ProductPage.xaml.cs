using InventoryPOS.Application.DTOs.Products;
using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace InventoryPOS.Desktop.Views;

public sealed partial class ProductsPage : Page
{
    public ProductsViewModel ViewModel { get; }

    public ProductsPage()
    {
        InitializeComponent();

        ViewModel =
            App.Services.GetRequiredService<ProductsViewModel>();

        DataContext = ViewModel;
    }

    protected override async void OnNavigatedTo(
        NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        await ViewModel.LoadAsync();
    }

    private void AddProductButton_Click(
    object sender,
    RoutedEventArgs e)
    {
        Frame.Navigate(typeof(AddProductPage));
    }

    private void ProductsList_ItemClick(
        object sender,
        ItemClickEventArgs e)
    {
        if (e.ClickedItem is not ProductListItemDto product)
            return;

        Frame.Navigate(
            typeof(ProductDetailsPage),
            product.Id);
    }
}