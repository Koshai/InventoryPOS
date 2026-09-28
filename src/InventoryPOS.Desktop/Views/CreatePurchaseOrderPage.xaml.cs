using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace InventoryPOS.Desktop.Views;

public sealed partial class CreatePurchaseOrderPage : Page
{
    public CreatePurchaseOrderViewModel ViewModel { get; }

    public CreatePurchaseOrderPage()
    {
        InitializeComponent();

        ViewModel =
            App.Services
                .GetRequiredService<CreatePurchaseOrderViewModel>();

        DataContext = ViewModel;
    }

    protected override async void OnNavigatedTo(
    NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is Guid requisitionId &&
            requisitionId != Guid.Empty)
        {
            await ViewModel
                .LoadFromRequisitionAsync(
                    requisitionId);

            return;
        }

        await ViewModel.LoadAsync();
    }

    private void AddProductButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ViewModel.AddLine();
    }

    private void RemoveLineButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.Tag is not
            PurchaseOrderLineEditViewModel line)
            return;

        ViewModel.RemoveLine(line);
    }

    private async void CreatePurchaseOrderButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ErrorInfoBar.IsOpen = false;

        var purchaseOrderId =
            await ViewModel.SaveAsync();

        if (!purchaseOrderId.HasValue)
        {
            ErrorInfoBar.IsOpen =
                !string.IsNullOrWhiteSpace(
                    ViewModel.ErrorMessage);

            return;
        }

        Frame.Navigate(
            typeof(PurchaseOrderDetailsPage),
            purchaseOrderId.Value);
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