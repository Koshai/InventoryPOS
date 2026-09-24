using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace InventoryPOS.Desktop.Views;

public sealed partial class SettingsPage : Page
{
    private bool _hasLoaded;

    public BusinessProfileViewModel ViewModel { get; }

    public SettingsPage()
    {
        InitializeComponent();

        ViewModel =
            App.Services.GetRequiredService<BusinessProfileViewModel>();

        DataContext = ViewModel;
    }

    protected override async void OnNavigatedTo(
    NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (_hasLoaded)
            return;

        _hasLoaded = true;

        await ViewModel.LoadAsync();

        await RefreshLogoPreviewAsync();
    }

    private async void SaveButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        SaveButton.IsEnabled = false;

        try
        {
            await ViewModel.SaveAsync();
        }
        finally
        {
            SaveButton.IsEnabled = true;
        }
    }

    private async void ChooseLogoButton_Click(
    object sender,
    RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();

        picker.ViewMode =
            PickerViewMode.Thumbnail;

        picker.SuggestedStartLocation =
            PickerLocationId.PicturesLibrary;

        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");

        var mainWindow =
            App.Services.GetRequiredService<MainWindow>();

        var hwnd =
            WinRT.Interop.WindowNative.GetWindowHandle(
                mainWindow);

        WinRT.Interop.InitializeWithWindow.Initialize(
            picker,
            hwnd);

        var file =
            await picker.PickSingleFileAsync();

        if (file is null)
            return;

        await ViewModel.SetLogoAsync(file);

        if (ViewModel.HasLogo)
        {
            await ViewModel.SaveAsync();
        }

        await RefreshLogoPreviewAsync();
    }

    private async void RemoveLogoButton_Click(
    object sender,
    RoutedEventArgs e)
    {
        await ViewModel.RemoveLogoAsync();

        await ViewModel.SaveAsync();

        ClearLogoPreview();
    }

    private async Task RefreshLogoPreviewAsync()
    {
        ClearLogoPreview();

        var path =
            ViewModel.LogoAbsolutePath;

        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            var file =
                await Windows.Storage.StorageFile
                    .GetFileFromPathAsync(path);

            await using var stream =
                await file.OpenStreamForReadAsync();

            var bitmap =
                new BitmapImage();

            await bitmap.SetSourceAsync(
                stream.AsRandomAccessStream());

            LogoPreviewImage.Source = bitmap;

            LogoPlaceholderIcon.Visibility =
                Visibility.Collapsed;
        }
        catch
        {
            ClearLogoPreview();
        }
    }

    private void ClearLogoPreview()
    {
        LogoPreviewImage.Source = null;

        LogoPlaceholderIcon.Visibility =
            Visibility.Visible;
    }
}