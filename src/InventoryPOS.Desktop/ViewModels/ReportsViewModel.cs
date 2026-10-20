using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using InventoryPOS.Application.DTOs.Inventory;
using InventoryPOS.Application.DTOs.Reports;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Application.Reports;
using Microsoft.UI.Xaml;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace InventoryPOS.Desktop.ViewModels;

public sealed class ReportsViewModel : INotifyPropertyChanged
{
    private readonly IReportQueryService _reports;
    private readonly ISaleService _saleService;

    private bool _isLoading;
    private string? _errorMessage;
    private string? _successMessage;
    private string _selectedPreset = "thismonth";
    private LocationLookupDto? _selectedLocation;
    private SalesSummaryReportDto? _salesSummary;
    private InventoryOnHandReportDto? _inventory;

    public ReportsViewModel(IReportQueryService reports, ISaleService saleService)
    {
        _reports = reports;
        _saleService = saleService;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<LocationLookupDto> Locations { get; } = new();
    public ObservableCollection<string> PeriodPresets { get; } = new()
    {
        "today", "yesterday", "last7", "thismonth"
    };

    public ObservableCollection<InventoryOnHandLineDto> InventoryLines { get; } = new();

    public string SelectedPreset
    {
        get => _selectedPreset;
        set => SetProperty(ref _selectedPreset, value);
    }

    public LocationLookupDto? SelectedLocation
    {
        get => _selectedLocation;
        set => SetProperty(ref _selectedLocation, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
                OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public string? SuccessMessage
    {
        get => _successMessage;
        private set
        {
            if (SetProperty(ref _successMessage, value))
                OnPropertyChanged(nameof(HasSuccess));
        }
    }

    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);

    public SalesSummaryReportDto? SalesSummary
    {
        get => _salesSummary;
        private set
        {
            if (SetProperty(ref _salesSummary, value))
            {
                OnPropertyChanged(nameof(SalesSummaryText));
            }
        }
    }

    public string SalesSummaryText
    {
        get
        {
            if (SalesSummary is null)
                return "Run the sales summary report to see totals.";

            var s = SalesSummary;
            return
                $"Transactions: {s.TransactionCount}\n" +
                $"Gross: ৳{s.GrossSales:N2}\n" +
                $"Discounts: ৳{s.DiscountAmount:N2}\n" +
                $"Tax: ৳{s.TaxAmount:N2}\n" +
                $"Invoice total: ৳{s.InvoiceTotal:N2}\n" +
                $"Returns: ৳{s.SalesReturnsAmount:N2}\n" +
                $"Net sales: ৳{s.NetSales:N2}\n" +
                $"Payments collected: ৳{s.PaymentsCollected:N2}\n" +
                $"Avg ticket: ৳{s.AverageTransactionValue:N2}";
        }
    }

    public async Task LoadLookupsAsync()
    {
        var locations = await _saleService.GetLocationsAsync();
        Locations.Clear();
        Locations.Add(new LocationLookupDto { Id = Guid.Empty, Name = "All locations" });
        foreach (var location in locations)
            Locations.Add(location);
        SelectedLocation ??= Locations.FirstOrDefault();
    }

    public async Task RunSalesSummaryAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;

            var filter = BuildFilter();
            SalesSummary = await _reports.GetSalesSummaryAsync(filter);
            SuccessMessage = "Sales summary loaded.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task RunInventoryAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;

            Guid? locationId = SelectedLocation is null || SelectedLocation.Id == Guid.Empty
                ? null
                : SelectedLocation.Id;

            _inventory = await _reports.GetInventoryOnHandAsync(locationId);
            InventoryLines.Clear();
            foreach (var line in _inventory.Lines)
                InventoryLines.Add(line);

            SuccessMessage =
                $"Inventory on hand: {_inventory.SkuCount} SKUs, " +
                $"value ৳{_inventory.InventoryValueAtCost:N2}, " +
                $"low stock {_inventory.LowStockCount}, near expiry {_inventory.NearExpiryBatchCount}.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task ExportSalesSummaryCsvAsync(Window window)
    {
        if (SalesSummary is null)
        {
            ErrorMessage = "Run the sales summary first.";
            return;
        }

        var picker = new FileSavePicker();
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeChoices.Add("CSV", new[] { ".csv" });
        picker.SuggestedFileName = "sales-summary.csv";

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSaveFileAsync();
        if (file is null)
            return;

        var s = SalesSummary;
        var csv = new StringBuilder();
        csv.Append('\uFEFF'); // UTF-8 BOM for Excel/Bangla
        csv.AppendLine("Metric,Value");
        csv.AppendLine($"FromUtc,{s.FromUtc:o}");
        csv.AppendLine($"ToUtc,{s.ToUtc:o}");
        csv.AppendLine($"TransactionCount,{s.TransactionCount}");
        csv.AppendLine($"GrossSales,{s.GrossSales}");
        csv.AppendLine($"DiscountAmount,{s.DiscountAmount}");
        csv.AppendLine($"TaxAmount,{s.TaxAmount}");
        csv.AppendLine($"InvoiceTotal,{s.InvoiceTotal}");
        csv.AppendLine($"SalesReturnsAmount,{s.SalesReturnsAmount}");
        csv.AppendLine($"NetSales,{s.NetSales}");
        csv.AppendLine($"PaymentsCollected,{s.PaymentsCollected}");
        csv.AppendLine($"AverageTransactionValue,{s.AverageTransactionValue}");

        await FileIO.WriteTextAsync(file, csv.ToString());
        SuccessMessage = "CSV exported: " + file.Path;
    }

    private ReportFilterDto BuildFilter()
    {
        var offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
        var filter = ReportPeriodHelper.CreateLocalDayRange(DateTime.Now, offset, SelectedPreset);
        if (SelectedLocation is not null && SelectedLocation.Id != Guid.Empty)
            filter.LocationId = SelectedLocation.Id;
        return filter;
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
