using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace InventoryPOS.Desktop.Views;

public sealed partial class PurchaseReturnDetailsPage : Page
{
    public PurchaseReturnDetailsViewModel ViewModel { get; }

    public PurchaseReturnDetailsPage()
    {
        InitializeComponent();

        ViewModel =
            App.Services.GetRequiredService<
                PurchaseReturnDetailsViewModel>();
    }

    protected override async void OnNavigatedTo(
        NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is Guid purchaseReturnId &&
            purchaseReturnId != Guid.Empty)
        {
            await ViewModel.LoadAsync(
                purchaseReturnId);
        }
    }

    private void Back_Click(
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
                typeof(PurchaseReturnHistoryPage));
        }
    }
}