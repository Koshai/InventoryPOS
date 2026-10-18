using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using InventoryPOS.LicenseManager.Data;
using InventoryPOS.Licensing.Issuer;
using Microsoft.Win32;

namespace InventoryPOS.LicenseManager;

public partial class MainWindow : Window
{
    private readonly LicenseManagerDb _db;
    private readonly LicenseKeyStore _keys;
    private readonly LicenseIssuerService _issuer;
    private readonly string _outputDirectory;

    private CustomerRow? _selectedCustomer;
    private MachineLicenseRow? _selectedMachine;

    public MainWindow()
    {
        InitializeComponent();

        _db = new LicenseManagerDb(LicenseManagerDb.DefaultDatabasePath);
        _keys = new LicenseKeyStore();
        _keys.EnsureMigratedFromLegacyAppData();
        _issuer = new LicenseIssuerService(_keys);

        _outputDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "InventoryPOS",
            "LicenseManager",
            "issued-licenses");

        RefreshKeysStatus();
        ReloadCustomers();
    }

    private void RefreshKeysStatus()
    {
        if (_keys.HasKeys)
        {
            KeysStatusText.Text =
                $"Signing keys ready. Private key: {_keys.PrivateKeyPath}\n" +
                "Back up the private key. The public key in the POS app (TrustedLicenseKeys) must match.";
            GenerateKeysButton.IsEnabled = false;
        }
        else
        {
            KeysStatusText.Text =
                "No signing keys found. Click “Generate signing keys” once, then embed the public key into the POS app before shipping.";
            GenerateKeysButton.IsEnabled = true;
        }
    }

    private void ReloadCustomers(string? selectEmail = null)
    {
        var customers = _db.SearchCustomers(SearchBox.Text);
        CustomerList.ItemsSource = customers;

        if (!string.IsNullOrWhiteSpace(selectEmail))
        {
            var match = customers.FirstOrDefault(c =>
                string.Equals(c.Email, selectEmail, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
                CustomerList.SelectedItem = match;
        }
    }

    private void Search_Click(object sender, RoutedEventArgs e) => ReloadCustomers();

    private void CustomerList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedCustomer = CustomerList.SelectedItem as CustomerRow;
        if (_selectedCustomer is null)
        {
            MachineGrid.ItemsSource = null;
            HistoryGrid.ItemsSource = null;
            return;
        }

        EmailBox.Text = _selectedCustomer.Email;
        NameBox.Text = _selectedCustomer.DisplayName;
        ReloadMachines();
    }

    private void ReloadMachines()
    {
        if (_selectedCustomer is null)
        {
            MachineGrid.ItemsSource = null;
            return;
        }

        var machines = _db.GetMachines(_selectedCustomer.Id);
        MachineGrid.ItemsSource = machines;
        if (machines.Count > 0)
            MachineGrid.SelectedIndex = 0;
        else
            HistoryGrid.ItemsSource = null;
    }

    private void MachineGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedMachine = MachineGrid.SelectedItem as MachineLicenseRow;
        if (_selectedMachine is null)
        {
            HistoryGrid.ItemsSource = null;
            return;
        }

        InstallationIdBox.Text = _selectedMachine.InstallationId;
        HistoryGrid.ItemsSource = _db.GetHistory(_selectedMachine.Id);
    }

    private void GenerateKeys_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _keys.GenerateKeys();
            RefreshKeysStatus();
            StatusText.Text =
                "Keys created. Back up the private key now. Copy the public key PEM into TrustedLicenseKeys before shipping builds.";
            MessageBox.Show(
                this,
                $"Private key:\n{_keys.PrivateKeyPath}\n\nPublic key:\n{_keys.PublicKeyPath}\n\n" +
                "Embed the public key into InventoryPOS.Desktop TrustedLicenseKeys before shipping.",
                "Signing keys generated",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Generate keys failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Issue_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!_keys.HasKeys)
            {
                MessageBox.Show(
                    this,
                    "Generate signing keys before issuing licenses.",
                    "Keys required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var email = EmailBox.Text.Trim();
            var name = string.IsNullOrWhiteSpace(NameBox.Text) ? email : NameBox.Text.Trim();
            var installationId = InstallationIdBox.Text.Trim();

            if (!int.TryParse(DaysBox.Text.Trim(), out var days) || days < 1)
                throw new InvalidOperationException("Enter a valid duration in days.");

            var customer = _db.UpsertCustomer(email, name);
            var license = _issuer.CreateBetaLicense(email, name, installationId, days);
            var signed = _issuer.Sign(license);
            var path = _issuer.WriteLicenseFile(signed, license.LicenseId, _outputDirectory);

            var machine = _db.SaveIssuedLicense(
                customer.Id,
                license.InstallationId,
                license.Edition.ToString(),
                license.LicenseId,
                license.IssuedAtUtc,
                license.ValidFromUtc,
                license.ExpiresAtUtc!.Value,
                JsonSerializer.Serialize(license.Features),
                path,
                "Issued from License Manager");

            ReloadCustomers(customer.Email);
            CustomerList.SelectedItem = _db.SearchCustomers(customer.Email)
                .FirstOrDefault(c => c.Id == customer.Id);
            ReloadMachines();
            MachineGrid.SelectedItem = (_db.GetMachines(customer.Id))
                .FirstOrDefault(m => m.Id == machine.Id);

            StatusText.Text =
                $"License issued for {email} / {installationId}. Expires {license.ExpiresAtUtc:yyyy-MM-dd} UTC.\n{path}";

            MessageBox.Show(
                this,
                $"License file created:\n{path}\n\nEmail this file to the customer.",
                "License issued",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Issue failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        var path = _selectedMachine?.LastIssuedFilePath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            Directory.CreateDirectory(_outputDirectory);
            Process.Start("explorer.exe", _outputDirectory);
            return;
        }

        Process.Start("explorer.exe", $"/select,\"{path}\"");
    }

    private void CopyPath_Click(object sender, RoutedEventArgs e)
    {
        var path = _selectedMachine?.LastIssuedFilePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            StatusText.Text = "No license file path to copy.";
            return;
        }

        Clipboard.SetText(path);
        StatusText.Text = "License file path copied to clipboard.";
    }

    private void Verify_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!_keys.HasKeys)
            {
                MessageBox.Show(this, "Generate or restore signing keys first.", "Keys required",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new OpenFileDialog
            {
                Filter = "InventoryPOS license (*.iposlic)|*.iposlic",
                Title = "Select license file to verify"
            };

            if (dialog.ShowDialog(this) != true)
                return;

            var installationId = InstallationIdBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(installationId))
            {
                MessageBox.Show(this, "Paste the Installation ID to verify against.", "Installation ID required",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = _issuer.VerifyFile(dialog.FileName, installationId);
            MessageBox.Show(
                this,
                $"{result.Status}\n{result.Message}",
                "Verification result",
                MessageBoxButton.OK,
                result.IsUsable ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Verify failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
