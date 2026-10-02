using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;

namespace InventoryPOS.Desktop.Views;

public sealed partial class SalesQuoteHistoryPage :
    Page
{
    public SalesQuoteHistoryViewModel
        ViewModel
    { get; }

    public SalesQuoteHistoryPage()
    {
        ViewModel =
            App.Services.GetRequiredService<
                SalesQuoteHistoryViewModel>();

        InitializeComponent();

        DataContext =
            ViewModel;
    }

    protected override async void OnNavigatedTo(
        NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        await ViewModel.LoadAsync();

        ShowError();
    }

    // =========================================================
    // Search
    // =========================================================

    private async void SearchButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.SearchAsync();

        ShowError();
    }

    private async void ClearButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.ClearSearchAsync();

        ShowError();
    }

    private async void RefreshButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.LoadAsync();

        ShowError();
    }

    private async void SearchTextBox_KeyDown(
        object sender,
        KeyRoutedEventArgs e)
    {
        if (e.Key !=
            Windows.System.VirtualKey.Enter)
        {
            return;
        }

        await ViewModel.SearchAsync();

        ShowError();
    }

    // =========================================================
    // Quote
    // =========================================================

    private void Quote_ItemClick(
    object sender,
    ItemClickEventArgs e)
    {
        if (e.ClickedItem is not
            SalesQuoteHistoryItemViewModel quote)
        {
            return;
        }

        Frame.Navigate(
            typeof(SalesQuoteDetailsPage),
            quote.Id);
    }

    // =========================================================
    // Error
    // =========================================================

    private void ShowError()
    {
        ErrorInfoBar.IsOpen =
            !string.IsNullOrWhiteSpace(
                ViewModel.ErrorMessage);
    }
}