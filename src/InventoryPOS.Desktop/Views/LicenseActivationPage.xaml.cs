
using InventoryPOS.Application.Licensing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;

namespace InventoryPOS.Desktop.Views;

public sealed partial class LicenseActivationPage : Page
{
    private readonly IInstallationIdentityService _identity;
    private readonly ILicenseActivationService _activation;

    public LicenseActivationPage()
    {
        InitializeComponent();

        _identity = App.Services.GetRequiredService<
            IInstallationIdentityService>();

        _activation = App.Services.GetRequiredService<
            ILicenseActivationService>();

        try
        {
            InstallationIdBox.Text =
                _identity.GetOrCreateInstallationId();

            DisplayResult(
                _activation.GetCurrentLicenseStatus());
        }
        catch (Exception ex)
        {
            ActivateButton.IsEnabled = false;

            LicenseInfoBar.Severity =
                InfoBarSeverity.Error;

            LicenseInfoBar.Title =
                "Licensing initialization failed";

            LicenseInfoBar.Message = ex.Message;
        }
    }

    private void CopyInstallationId_Click(
        object sender,
        RoutedEventArgs e)
    {
        var package = new DataPackage();

        package.SetText(InstallationIdBox.Text);

        Clipboard.SetContent(package);
    }

    private async void ActivateButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();

        picker.FileTypeFilter.Add(".iposlic");

        var window = App.Services.GetRequiredService<
            MainWindow>();

        var hwnd =
            WinRT.Interop.WindowNative.GetWindowHandle(
                window);

        WinRT.Interop.InitializeWithWindow.Initialize(
            picker,
            hwnd);

        var file = await picker.PickSingleFileAsync();

        if (file is null)
            return;

        ActivateButton.IsEnabled = false;

        try
        {
            var result =
                _activation.ActivateFromFile(file.Path);

            DisplayResult(result);
        }
        catch (Exception ex)
        {
            LicenseInfoBar.IsOpen = true;
            LicenseInfoBar.Severity =
                InfoBarSeverity.Error;
            LicenseInfoBar.Title =
                "Activation failed";
            LicenseInfoBar.Message = ex.Message;
        }
        finally
        {
            ActivateButton.IsEnabled = true;
        }
    }

    private void DisplayResult(
        LicenseValidationResult result)
    {
        LicenseInfoBar.IsOpen = true;

        LicenseInfoBar.Title =
            result.Status.ToString();

        LicenseInfoBar.Message =
            result.Message;

        LicenseInfoBar.Severity =
            result.IsUsable
                ? InfoBarSeverity.Success
                : InfoBarSeverity.Warning;

        if (result.License is null)
        {
            LicenseDetailsText.Text =
                "No active license details available.";

            return;
        }

        var license = result.License;

        string expiry = license.ExpiresAtUtc.HasValue
            ? license.ExpiresAtUtc.Value
                .ToLocalTime()
                .ToString("yyyy-MM-dd")
            : "No expiration";

        LicenseDetailsText.Text =
            $"Customer: {license.CustomerName}\n" +
            $"Edition: {license.Edition}\n" +
            $"Expires: {expiry}";
    }
}
