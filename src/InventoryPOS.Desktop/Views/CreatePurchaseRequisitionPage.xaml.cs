using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace InventoryPOS.Desktop.Views;

public sealed partial class CreatePurchaseRequisitionPage : Page
{
    public CreatePurchaseRequisitionViewModel ViewModel { get; }

    public CreatePurchaseRequisitionPage()
    {
        InitializeComponent();

        ViewModel =
            App.Services.GetRequiredService<
                CreatePurchaseRequisitionViewModel>();
    }

    protected override async void OnNavigatedTo(
        NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        await ViewModel.LoadAsync();
    }

    private void AddLine_Click(
        object sender,
        RoutedEventArgs e)
    {
        ViewModel.AddLine();
    }

    private void RemoveLine_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is Button button &&
            button.Tag is PurchaseRequisitionLineEditViewModel line)
        {
            ViewModel.RemoveLine(line);
        }
    }

    private async void Save_Click(
        object sender,
        RoutedEventArgs e)
    {
        var requisitionId =
            await ViewModel.SaveAsync();

        if (!requisitionId.HasValue)
            return;

        var dialog =
            new ContentDialog
            {
                XamlRoot = XamlRoot,

                Title = "Requisition created",

                Content =
                    "The purchase requisition was created successfully.",

                PrimaryButtonText = "OK"
            };

        await dialog.ShowAsync();

        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
        else
        {
            Frame.Navigate(
                typeof(PurchaseOrdersPage));
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
                typeof(PurchaseOrdersPage));
        }
    }
}