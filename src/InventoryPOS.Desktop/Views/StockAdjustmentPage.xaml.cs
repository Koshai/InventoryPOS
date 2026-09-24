using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace InventoryPOS.Desktop.Views;

public sealed partial class StockAdjustmentPage : Page
{
    public StockAdjustmentViewModel ViewModel { get; }

    public StockAdjustmentPage()
    {
        InitializeComponent();

        ViewModel =
            App.Services
                .GetRequiredService<StockAdjustmentViewModel>();

        DataContext =
            ViewModel;
    }

    protected override async void OnNavigatedTo(
        NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        await ViewModel.LoadAsync();

        UpdateInfoBars();
    }

    private async void SaveButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ErrorInfoBar.IsOpen = false;
        SuccessInfoBar.IsOpen = false;

        await ViewModel.SaveAsync();

        UpdateInfoBars();
    }

    private void CancelButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }

    private void UpdateInfoBars()
    {
        ErrorInfoBar.IsOpen =
            !string.IsNullOrWhiteSpace(
                ViewModel.ErrorMessage);

        SuccessInfoBar.IsOpen =
            !string.IsNullOrWhiteSpace(
                ViewModel.SuccessMessage);
    }
}