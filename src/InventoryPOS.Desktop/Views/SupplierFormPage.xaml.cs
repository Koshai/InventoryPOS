using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace InventoryPOS.Desktop.Views;

public sealed partial class SupplierFormPage : Page
{
    public SupplierFormViewModel ViewModel { get; }

    public SupplierFormPage()
    {
        InitializeComponent();

        ViewModel =
            App.Services
                .GetRequiredService<SupplierFormViewModel>();

        DataContext = ViewModel;
    }

    protected override async void OnNavigatedTo(
        NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        Guid? supplierId =
            e.Parameter is Guid id
                ? id
                : null;

        await ViewModel.LoadAsync(
            supplierId);

        ErrorInfoBar.IsOpen =
            !string.IsNullOrWhiteSpace(
                ViewModel.ErrorMessage);
    }

    private async void SaveButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ErrorInfoBar.IsOpen = false;

        var success =
            await ViewModel.SaveAsync();

        if (!success)
        {
            ErrorInfoBar.IsOpen =
                !string.IsNullOrWhiteSpace(
                    ViewModel.ErrorMessage);

            return;
        }

        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
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
}