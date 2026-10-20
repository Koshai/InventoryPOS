using InventoryPOS.Application.Interfaces;
using InventoryPOS.Application.Updates;
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
    private readonly IAppUpdateService _appUpdateService;
    private AppUpdateInfo? _pendingOnlineUpdate;

    public BusinessProfileViewModel ViewModel { get; }

    public SettingsPage()
    {
        InitializeComponent();

        ViewModel =
            App.Services.GetRequiredService<BusinessProfileViewModel>();

        _appUpdateService =
            App.Services.GetRequiredService<IAppUpdateService>();

        DataContext = ViewModel;

        AppVersionText.Text =
            "Installed version: " + _appUpdateService.GetCurrentVersion();
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

    private async void CheckUpdatesButton_Click(object sender, RoutedEventArgs e)
    {
        CheckUpdatesButton.IsEnabled = false;
        DownloadUpdateButton.Visibility = Visibility.Collapsed;
        _pendingOnlineUpdate = null;

        try
        {
            var result = await _appUpdateService.CheckForUpdatesAsync();
            _pendingOnlineUpdate = result;

            UpdateInfoBar.IsOpen = true;
            UpdateInfoBar.Title = result.UpdateAvailable ? "Update available" : "Update check";
            UpdateInfoBar.Message = result.Message;
            UpdateInfoBar.Severity = result.UpdateAvailable
                ? InfoBarSeverity.Informational
                : (result.IsOnlineCheckConfigured ? InfoBarSeverity.Success : InfoBarSeverity.Warning);

            if (result.UpdateAvailable && !string.IsNullOrWhiteSpace(result.DownloadUrl))
                DownloadUpdateButton.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            UpdateInfoBar.IsOpen = true;
            UpdateInfoBar.Severity = InfoBarSeverity.Error;
            UpdateInfoBar.Title = "Update check failed";
            UpdateInfoBar.Message = ex.Message;
        }
        finally
        {
            CheckUpdatesButton.IsEnabled = true;
        }
    }

    private async void UpgradeFromFileButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.SuggestedStartLocation = PickerLocationId.Downloads;
        picker.FileTypeFilter.Add(".exe");

        var mainWindow = App.Services.GetRequiredService<MainWindow>();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file is null)
            return;

        var confirm = new ContentDialog
        {
            Title = "Upgrade InventoryPOS?",
            Content =
                "This will install a newer InventoryPOSSetup.exe and replace program files only.\n\n" +
                "Your database, license, and settings will not be deleted.\n\n" +
                "The application will close so the installer can run.",
            PrimaryButtonText = "Upgrade",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        if (await confirm.ShowAsync() != ContentDialogResult.Primary)
            return;

        try
        {
            UpgradeFromFileButton.IsEnabled = false;
            await _appUpdateService.LaunchInstallerAndExitAsync(file.Path);
        }
        catch (Exception ex)
        {
            UpdateInfoBar.IsOpen = true;
            UpdateInfoBar.Severity = InfoBarSeverity.Error;
            UpdateInfoBar.Title = "Upgrade failed";
            UpdateInfoBar.Message = ex.Message;
            UpgradeFromFileButton.IsEnabled = true;
        }
    }

    private async void DownloadUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_pendingOnlineUpdate is null || !_pendingOnlineUpdate.UpdateAvailable)
            return;

        var confirm = new ContentDialog
        {
            Title = "Download and install update?",
            Content =
                $"Download version {_pendingOnlineUpdate.LatestVersion} and run the installer?\n\n" +
                "Your database, license, and settings will be preserved. The app will close afterward.",
            PrimaryButtonText = "Download & install",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        if (await confirm.ShowAsync() != ContentDialogResult.Primary)
            return;

        DownloadUpdateButton.IsEnabled = false;
        try
        {
            UpdateInfoBar.IsOpen = true;
            UpdateInfoBar.Severity = InfoBarSeverity.Informational;
            UpdateInfoBar.Title = "Downloading update…";
            UpdateInfoBar.Message = "Please wait. The app will close when the installer starts.";

            await _appUpdateService.DownloadAndLaunchUpdateAsync(_pendingOnlineUpdate);
        }
        catch (Exception ex)
        {
            UpdateInfoBar.IsOpen = true;
            UpdateInfoBar.Severity = InfoBarSeverity.Error;
            UpdateInfoBar.Title = "Download failed";
            UpdateInfoBar.Message = ex.Message;
            DownloadUpdateButton.IsEnabled = true;
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
