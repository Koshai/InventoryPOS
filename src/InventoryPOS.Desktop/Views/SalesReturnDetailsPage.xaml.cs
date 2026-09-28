using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace InventoryPOS.Desktop.Views;

public sealed partial class SalesReturnDetailsPage : Page
{
    public SalesReturnDetailsViewModel ViewModel { get; }

    public SalesReturnDetailsPage()
    {
        InitializeComponent();

        ViewModel =
            App.Services.GetRequiredService<SalesReturnDetailsViewModel>();

        DataContext = ViewModel;
    }

    protected override async void OnNavigatedTo(
        NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is Guid salesReturnId &&
            salesReturnId != Guid.Empty)
        {
            await ViewModel.LoadAsync(
                salesReturnId);
        }
    }

    private void BackButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
            return;
        }

        Frame.Navigate(
            typeof(SalesReturnsPage));
    }

    private void ViewInvoiceButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (ViewModel.Return is null)
            return;

        if (ViewModel.Return.SaleId ==
            Guid.Empty)
        {
            return;
        }

        Frame.Navigate(
            typeof(InvoicePage),
            ViewModel.Return.SaleId);
    }
}