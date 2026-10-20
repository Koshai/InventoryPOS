using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using InventoryPOS.Application.DTOs.Inventory;
using InventoryPOS.Application.DTOs.Reports;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Application.Reports;

namespace InventoryPOS.Desktop.ViewModels;

public sealed class DashboardViewModel : INotifyPropertyChanged
{
    private readonly IReportQueryService _reports;
    private readonly ISaleService _saleService;

    private bool _isLoading;
    private bool _suppressRefresh;
    private string? _errorMessage;
    private string _selectedPreset = "today";
    private LocationLookupDto? _selectedLocation;
    private DashboardSnapshotDto? _snapshot;
    private string _lastUpdatedText = string.Empty;

    public DashboardViewModel(IReportQueryService reports, ISaleService saleService)
    {
        _reports = reports;
        _saleService = saleService;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<LocationLookupDto> Locations { get; } = new();

    public ObservableCollection<string> PeriodPresets { get; } = new()
    {
        "today",
        "yesterday",
        "last7",
        "thismonth"
    };

    public string SelectedPreset
    {
        get => _selectedPreset;
        set
        {
            if (SetProperty(ref _selectedPreset, value) && !_suppressRefresh)
                _ = RefreshAsync();
        }
    }

    public LocationLookupDto? SelectedLocation
    {
        get => _selectedLocation;
        set
        {
            if (SetProperty(ref _selectedLocation, value) && !_suppressRefresh)
                _ = RefreshAsync();
        }
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

    public string LastUpdatedText
    {
        get => _lastUpdatedText;
        private set => SetProperty(ref _lastUpdatedText, value);
    }

    public DashboardSnapshotDto? Snapshot
    {
        get => _snapshot;
        private set
        {
            if (SetProperty(ref _snapshot, value))
            {
                OnPropertyChanged(nameof(GrossSalesText));
                OnPropertyChanged(nameof(NetSalesText));
                OnPropertyChanged(nameof(DiscountsText));
                OnPropertyChanged(nameof(ReturnsText));
                OnPropertyChanged(nameof(PaymentsText));
                OnPropertyChanged(nameof(OutstandingText));
                OnPropertyChanged(nameof(TransactionsText));
                OnPropertyChanged(nameof(AverageSaleText));
                OnPropertyChanged(nameof(LowStockText));
                OnPropertyChanged(nameof(NearExpiryText));
                OnPropertyChanged(nameof(PendingPurchasingText));
                OnPropertyChanged(nameof(WarrantyActionText));
                OnPropertyChanged(nameof(ProfitText));
                OnPropertyChanged(nameof(DailySales));
                OnPropertyChanged(nameof(TopProducts));
            }
        }
    }

    public string GrossSalesText => FormatMoney(Snapshot?.Sales.GrossSales);
    public string NetSalesText => FormatMoney(Snapshot?.Sales.NetSales);
    public string DiscountsText => FormatMoney(Snapshot?.Sales.DiscountAmount);
    public string ReturnsText => FormatMoney(Snapshot?.Sales.SalesReturnsAmount);
    public string PaymentsText => FormatMoney(Snapshot?.Sales.PaymentsCollected);
    public string OutstandingText => FormatMoney(Snapshot?.Receivables.OutstandingTotal);
    public string TransactionsText => Snapshot?.Sales.TransactionCount.ToString() ?? "—";
    public string AverageSaleText => FormatMoney(Snapshot?.Sales.AverageTransactionValue);
    public string LowStockText => Snapshot?.LowStockSkuCount.ToString() ?? "—";
    public string NearExpiryText => Snapshot?.NearExpiryBatchCount.ToString() ?? "—";
    public string PendingPurchasingText =>
        Snapshot is null
            ? "—"
            : $"{Snapshot.PendingPurchaseOrders} PO / {Snapshot.PendingPurchaseRequisitions} PR";
    public string WarrantyActionText => Snapshot?.WarrantyClaimsRequiringAction.ToString() ?? "—";
    public string ProfitText =>
        Snapshot?.GrossProfitAvailable == true
            ? FormatMoney(Snapshot.GrossProfit)
            : "Unavailable (COGS snapshot not recorded on sales yet)";

    public ObservableCollection<DailySalesPointDto> DailySales { get; } = new();
    public ObservableCollection<TopProductDto> TopProducts { get; } = new();

    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var locations = await _saleService.GetLocationsAsync();
            Locations.Clear();
            Locations.Add(new LocationLookupDto { Id = Guid.Empty, Name = "All locations" });
            foreach (var location in locations)
                Locations.Add(location);

            _suppressRefresh = true;
            SelectedLocation = Locations.FirstOrDefault();
            _suppressRefresh = false;
            await RefreshAsync();
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

    public async Task RefreshAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
            var filter = ReportPeriodHelper.CreateLocalDayRange(DateTime.Now, offset, SelectedPreset);
            if (SelectedLocation is not null && SelectedLocation.Id != Guid.Empty)
                filter.LocationId = SelectedLocation.Id;

            var snapshot = await _reports.GetDashboardSnapshotAsync(filter);
            Snapshot = snapshot;

            DailySales.Clear();
            foreach (var point in snapshot.DailySales)
                DailySales.Add(point);

            TopProducts.Clear();
            foreach (var product in snapshot.TopProductsByRevenue)
                TopProducts.Add(product);

            LastUpdatedText =
                "Updated " + snapshot.GeneratedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
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

    private static string FormatMoney(decimal? value) =>
        value.HasValue ? $"৳{value.Value:N2}" : "—";

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
