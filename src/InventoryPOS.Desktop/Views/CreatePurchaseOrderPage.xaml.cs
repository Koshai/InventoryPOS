using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Linq;

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
        var purchaseOrderId =
    await ViewModel.SaveAsync();

        if (!purchaseOrderId.HasValue)
            return;

        Frame.Navigate(
            typeof(PurchaseOrderDetailsPage),
            purchaseOrderId.Value);

        if (Frame.BackStack.Count > 0)
        {
            var createPage =
                Frame.BackStack.LastOrDefault(
                    x => x.SourcePageType ==
                        typeof(CreatePurchaseOrderPage));

            if (createPage is not null)
                Frame.BackStack.Remove(createPage);
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