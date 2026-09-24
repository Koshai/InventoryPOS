using InventoryPOS.Application.DTOs.Purchasing;
using InventoryPOS.Application.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.ViewModels;

public class PurchaseOrderDetailsViewModel : INotifyPropertyChanged
{
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly IGoodsReceiptService _goodsReceiptService;

    private PurchaseOrderDetailsDto? _purchaseOrder;
    private bool _isLoading;
    private string? _errorMessage;

    public ObservableCollection<GoodsReceiptListItemDto> Receipts
    {
        get;
    } = new();

    public PurchaseOrderDetailsDto? PurchaseOrder
    {
        get => _purchaseOrder;
        private set
        {
            if (_purchaseOrder == value)
                return;

            _purchaseOrder = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanReceiveGoods));
            OnPropertyChanged(nameof(StatusDisplay));
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (_isLoading == value)
                return;

            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (_errorMessage == value)
                return;

            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public bool CanReceiveGoods =>
        PurchaseOrder is not null &&
        PurchaseOrder.Lines.Any(x => x.RemainingQuantity > 0) &&
        !string.Equals(
            PurchaseOrder.Status,
            "Cancelled",
            StringComparison.OrdinalIgnoreCase);

    public string StatusDisplay =>
        PurchaseOrder?.Status ?? "Unknown";

    public PurchaseOrderDetailsViewModel(
    IPurchaseOrderService purchaseOrderService,
    IGoodsReceiptService goodsReceiptService)
    {
        _purchaseOrderService = purchaseOrderService;
        _goodsReceiptService = goodsReceiptService;
    }

    public async Task LoadAsync(Guid purchaseOrderId)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            PurchaseOrder =
                await _purchaseOrderService.GetByIdAsync(
                    purchaseOrderId);

            if (PurchaseOrder is null)
            {
                ErrorMessage =
                    "Purchase order could not be found.";
            }

            var receipts = await _goodsReceiptService.GetByPurchaseOrderAsync(purchaseOrderId);

            Receipts.Clear();

            foreach (var receipt in receipts)
            {
                Receipts.Add(receipt);
            }
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

    public event PropertyChangedEventHandler?
        PropertyChanged;

    private void OnPropertyChanged(
        [CallerMemberName]
        string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}