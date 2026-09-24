using InventoryPOS.Application.DTOs.Customers;
using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace InventoryPOS.Desktop.Views;

public sealed partial class CustomersPage : Page
{
    public CustomersViewModel ViewModel { get; }

    public CustomersPage()
    {
        InitializeComponent();

        ViewModel =
            App.Services.GetRequiredService<CustomersViewModel>();

        DataContext = ViewModel;
    }

    protected override async void OnNavigatedTo(
        NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        await ViewModel.LoadAsync();
    }

    private void AddCustomerButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Frame.Navigate(
            typeof(CustomerFormPage));
    }

    private void Customer_ItemClick(
        object sender,
        ItemClickEventArgs e)
    {
        if (e.ClickedItem is not CustomerListItemDto customer)
            return;

        Frame.Navigate(
            typeof(CustomerDetailsPage),
            customer.Id);
    }
}