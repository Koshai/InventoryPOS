using InventoryPOS.Application.Interfaces;
using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace InventoryPOS.Desktop.Views;

public sealed partial class EditProductPage : Page
{
    public EditProductViewModel ViewModel { get; }

    private readonly IProductImageService _productImageService;

    private Guid? _productId;

    private string? _selectedReplacementImagePath;

    private bool _removeImageRequested;

    public EditProductPage()
    {
        InitializeComponent();

        ViewModel =
            App.Services
                .GetRequiredService<EditProductViewModel>();

        _productImageService =
            App.Services
                .GetRequiredService<IProductImageService>();

        DataContext = ViewModel;
    }

    protected override async void OnNavigatedTo(
    NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is not Guid productId)
            return;

        _productId = productId;

        await ViewModel.LoadAsync(productId);

        ErrorInfoBar.IsOpen =
            !string.IsNullOrWhiteSpace(
                ViewModel.ErrorMessage);

        await LoadCurrentImageAsync();
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

        if (!_productId.HasValue)
            return;

        try
        {
            if (_removeImageRequested)
            {
                await _productImageService
                    .RemovePrimaryImageAsync(
                        _productId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(
                         _selectedReplacementImagePath))
            {
                await _productImageService
                    .SetPrimaryImageAsync(
                        _productId.Value,
                        _selectedReplacementImagePath);
            }
        }
        catch (Exception ex)
        {
            ErrorInfoBar.Message =
                $"Product was updated, but the image could not be updated: {ex.Message}";

            ErrorInfoBar.IsOpen = true;

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

    private async Task LoadCurrentImageAsync()
    {
        if (!_productId.HasValue)
            return;

        var imagePath =
            await _productImageService
                .GetPrimaryImagePathAsync(
                    _productId.Value);

        if (string.IsNullOrWhiteSpace(imagePath))
        {
            ProductImagePreview.Source = null;

            RemoveImageButton.Visibility =
                Visibility.Collapsed;

            return;
        }

        ProductImagePreview.Source =
            new BitmapImage(
                new Uri(imagePath));

        RemoveImageButton.Visibility =
            Visibility.Visible;
    }

    private async void ReplaceImageButton_Click(
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

        _selectedReplacementImagePath =
            file.Path;

        _removeImageRequested = false;

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
        _selectedReplacementImagePath = null;

        _removeImageRequested = true;

        ProductImagePreview.Source = null;

        RemoveImageButton.Visibility =
            Visibility.Collapsed;
    }
}