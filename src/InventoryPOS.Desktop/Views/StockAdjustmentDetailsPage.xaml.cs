using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace InventoryPOS.Desktop.Views;

public sealed partial class StockAdjustmentDetailsPage : Page
{
    public StockAdjustmentDetailsViewModel ViewModel { get; }

    public StockAdjustmentDetailsPage()
    {
        InitializeComponent();

        ViewModel =
            App.Services
                .GetRequiredService<StockAdjustmentDetailsViewModel>();

        DataContext =
            ViewModel;
    }

    protected override async void OnNavigatedTo(
        NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is not Guid adjustmentId)
            return;

        await ViewModel.LoadAsync(
            adjustmentId);

        ErrorInfoBar.IsOpen =
            !string.IsNullOrWhiteSpace(
                ViewModel.ErrorMessage);
    }

    private void BackButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }
}