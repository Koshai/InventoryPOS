using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace InventoryPOS.Desktop.Views;

public sealed partial class CustomerFormPage : Page
{
    public CustomerFormViewModel ViewModel { get; }

    public CustomerFormPage()
    {
        InitializeComponent();

        ViewModel =
            App.Services.GetRequiredService<CustomerFormViewModel>();

        DataContext = ViewModel;
    }

    protected override async void OnNavigatedTo(
        NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        Guid? customerId =
            e.Parameter is Guid id
                ? id
                : null;

        await ViewModel.LoadAsync(customerId);

        ShowError();
    }

    private async void SaveButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ErrorInfoBar.IsOpen = false;

        var customerId =
            await ViewModel.SaveAsync();

        if (!customerId.HasValue)
        {
            ShowError();
            return;
        }

        Frame.Navigate(
            typeof(CustomerDetailsPage),
            customerId.Value);
    }

    private void CancelButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
            Frame.GoBack();
        else
            Frame.Navigate(typeof(CustomersPage));
    }

    private void ShowError()
    {
        ErrorInfoBar.IsOpen =
            !string.IsNullOrWhiteSpace(
                ViewModel.ErrorMessage);
    }
}