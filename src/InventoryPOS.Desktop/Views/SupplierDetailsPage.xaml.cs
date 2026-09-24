using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace InventoryPOS.Desktop.Views;

public sealed partial class SupplierDetailsPage : Page
{
    private Guid? _supplierId;

    public SupplierDetailsViewModel ViewModel { get; }

    public SupplierDetailsPage()
    {
        InitializeComponent();

        ViewModel =
            App.Services
                .GetRequiredService<SupplierDetailsViewModel>();

        DataContext = ViewModel;
    }

    protected override async void OnNavigatedTo(
        NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is Guid supplierId)
        {
            _supplierId = supplierId;
        }

        if (!_supplierId.HasValue)
            return;

        await ViewModel.LoadAsync(
            _supplierId.Value);

        ErrorInfoBar.IsOpen =
            !string.IsNullOrWhiteSpace(
                ViewModel.ErrorMessage);
    }

    private void EditButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!_supplierId.HasValue)
            return;

        Frame.Navigate(
            typeof(SupplierFormPage),
            _supplierId.Value);
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