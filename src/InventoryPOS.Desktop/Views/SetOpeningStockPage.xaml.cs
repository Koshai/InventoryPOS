using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace InventoryPOS.Desktop.Views;

public sealed partial class SetOpeningStockPage : Page
{
    public SetOpeningStockViewModel ViewModel { get; }

    public SetOpeningStockPage()
    {
        InitializeComponent();

        ViewModel =
            App.Services
                .GetRequiredService<SetOpeningStockViewModel>();

        DataContext = ViewModel;
    }

    protected override async void OnNavigatedTo(
        NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        await ViewModel.LoadAsync();
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