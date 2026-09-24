using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace InventoryPOS.Desktop.Views;

public sealed partial class CustomerDetailsPage : Page
{
    private Guid _customerId;

    public CustomerDetailsViewModel ViewModel { get; }

    public CustomerDetailsPage()
    {
        InitializeComponent();

        ViewModel =
            App.Services.GetRequiredService<CustomerDetailsViewModel>();

        DataContext = ViewModel;
    }

    protected override async void OnNavigatedTo(
        NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is not Guid customerId)
            return;

        _customerId = customerId;

        await ViewModel.LoadAsync(customerId);

        ErrorInfoBar.IsOpen =
            !string.IsNullOrWhiteSpace(
                ViewModel.ErrorMessage);
    }

    private void ViewAccountButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_customerId == Guid.Empty)
            return;

        Frame.Navigate(
            typeof(CustomerAccountPage),
            _customerId);
    }

    private void EditButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_customerId == Guid.Empty)
            return;

        Frame.Navigate(
            typeof(CustomerFormPage),
            _customerId);
    }

    private void BackButton_Click(
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
                typeof(CustomersPage));
        }
    }
}