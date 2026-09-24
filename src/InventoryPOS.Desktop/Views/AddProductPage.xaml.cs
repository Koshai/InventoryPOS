using InventoryPOS.Application.Interfaces;
using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace InventoryPOS.Desktop.Views;

public sealed partial class AddProductPage : Page
{
    public AddProductViewModel ViewModel { get; }
    private string? _selectedImagePath;
    private readonly IProductImageService _productImageService;

    public AddProductPage()
    {
        InitializeComponent();

        ViewModel =
            App.Services.GetRequiredService<AddProductViewModel>();

        _productImageService =
            App.Services.GetRequiredService<IProductImageService>();

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

        var productId =
            await ViewModel.SaveAsync();

        if (productId is null)
        {
            ErrorInfoBar.IsOpen =
                !string.IsNullOrWhiteSpace(
                    ViewModel.ErrorMessage);

            return;
        }

        if (!string.IsNullOrWhiteSpace(
            _selectedImagePath))
        {
            try
            {
                await _productImageService
                    .SetPrimaryImageAsync(
                        productId.Value,
                        _selectedImagePath);
            }
            catch (Exception ex)
            {
                // Important:
                // the product itself has already been saved.
                ErrorInfoBar.Message =
                    $"Product was saved, but the image could not be saved: {ex.Message}";

                ErrorInfoBar.IsOpen = true;

                return;
            }
        }

        Frame.GoBack();
    }

    private void CancelButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Frame.GoBack();
    }

    private async void ChooseImageButton_Click(
    object sender,
    RoutedEventArgs e)
    {
        var picker =
            new FileOpenPicker();

        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".webp");

        var window =
            (MainWindow)App.Services
                .GetRequiredService<MainWindow>();

        var hwnd =
            WindowNative.GetWindowHandle(window);

        InitializeWithWindow.Initialize(
            picker,
            hwnd);

        var file =
            await picker.PickSingleFileAsync();

        if (file is null)
            return;

        _selectedImagePath =
            file.Path;

        ProductImagePreview.Source =
            new BitmapImage(
                new Uri(file.Path));

        RemoveImageButton.Visibility =
            Visibility.Visible;
    }

    private void RemoveImageButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        _selectedImagePath = null;

        ProductImagePreview.Source =
            null;

        RemoveImageButton.Visibility =
            Visibility.Collapsed;
    }
}