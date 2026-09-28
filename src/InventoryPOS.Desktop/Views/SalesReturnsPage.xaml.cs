using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace InventoryPOS.Desktop.Views;

public sealed partial class SalesReturnsPage : Page
{
    public SalesReturnsViewModel ViewModel { get; }

    public SalesReturnsPage()
    {
        InitializeComponent();

        ViewModel =
            App.Services.GetRequiredService<SalesReturnsViewModel>();

        DataContext =
            ViewModel;
    }

    protected override async void OnNavigatedTo(
        NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        await ViewModel.LoadAsync();
    }

    private void BackToSalesButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
            return;
        }

        Frame.Navigate(
            typeof(SalesPage));
    }

    private void ViewReturnButton_Click(
    object sender,
    RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.Tag is Guid salesReturnId &&
            salesReturnId != Guid.Empty)
        {
            Frame.Navigate(
                typeof(SalesReturnDetailsPage),
                salesReturnId);
        }
    }

    private async void PreviousButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.PreviousPageAsync();
    }

    private async void NextButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.NextPageAsync();
    }
}