using InventoryPOS.Application.DTOs.Purchasing;
using InventoryPOS.Application.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.ViewModels;

public class GoodsReceiptViewModel :
    INotifyPropertyChanged
{
    private readonly IGoodsReceiptService
        _goodsReceiptService;

    private ReceivePurchaseOrderDto?
        _purchaseOrder;

    private DateTimeOffset?
    _receiptDate =
        DateTimeOffset.Now.Date;

    private string? _notes;

    private bool _isLoading;

    private bool _isSaving;

    private string? _errorMessage;

    public ReceivePurchaseOrderDto?
        PurchaseOrder
    {
        get => _purchaseOrder;
        private set
        {
            _purchaseOrder = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<
        GoodsReceiptLineViewModel>
        Lines
    { get; } = new();

    public DateTimeOffset? ReceiptDate
    {
        get => _receiptDate;
        set
        {
            _receiptDate = value;
            OnPropertyChanged();
        }
    }

    public string? Notes
    {
        get => _notes;
        set
        {
            _notes = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public bool IsSaving
    {
        get => _isSaving;
        private set
        {
            _isSaving = value;
            OnPropertyChanged();
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public string ReceiptTotalDisplay =>
        $"৳{Lines.Sum(x =>
            Convert.ToDecimal(
                x.ReceivingQuantity) *
            x.UnitCost):N2}";

    public GoodsReceiptViewModel(
        IGoodsReceiptService goodsReceiptService)
    {
        _goodsReceiptService =
            goodsReceiptService;
    }

    public async Task LoadAsync(
        Guid purchaseOrderId)
    {
        try
        {
            IsLoading = true;

            ErrorMessage = null;

            PurchaseOrder =
                await _goodsReceiptService
                    .GetPurchaseOrderForReceiptAsync(
                        purchaseOrderId);

            if (PurchaseOrder is null)
            {
                ErrorMessage =
                    "Purchase order could not be found.";

                return;
            }

            Lines.Clear();

            foreach (var sourceLine
                     in PurchaseOrder.Lines)
            {
                var line =
                    new GoodsReceiptLineViewModel(
                        sourceLine);

                line.PropertyChanged +=
                    Line_PropertyChanged;

                Lines.Add(line);
            }

            OnPropertyChanged(
                nameof(ReceiptTotalDisplay));
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

    private void Line_PropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName ==
            nameof(
                GoodsReceiptLineViewModel
                    .ReceivingQuantity))
        {
            OnPropertyChanged(
                nameof(ReceiptTotalDisplay));
        }
    }

    public void ReceiveAll()
    {
        foreach (var line in Lines)
        {
            line.ReceivingQuantity =
                Convert.ToDouble(
                    line.RemainingQuantity);
        }
    }

    public async Task<Guid?> SaveAsync()
    {
        try
        {
            IsSaving = true;

            ErrorMessage = null;

            if (PurchaseOrder is null)
                return null;

            if (!ReceiptDate.HasValue)
            {
                ErrorMessage =
                    "Receipt date is required.";

                return null;
            }

            var receiving =
                Lines
                    .Where(x =>
                        x.ReceivingQuantity > 0)
                    .ToList();

            if (receiving.Count == 0)
            {
                ErrorMessage =
                    "Enter a receiving quantity for at least one product.";

                return null;
            }

            foreach (var line in receiving)
            {
                if (Convert.ToDecimal(
                        line.ReceivingQuantity) >
                    line.RemainingQuantity)
                {
                    ErrorMessage =
                        $"{line.ProductName}: receiving quantity exceeds remaining quantity.";

                    return null;
                }

                if (string.IsNullOrWhiteSpace(
                        line.SupplierBatchNumber))
                {
                    ErrorMessage =
                        $"{line.ProductName}: supplier batch number is required.";

                    return null;
                }

                if (line.ManufactureDate.HasValue &&
                    line.ExpiryDate.HasValue &&
                    line.ExpiryDate <
                    line.ManufactureDate)
                {
                    ErrorMessage =
                        $"{line.ProductName}: expiry date cannot be earlier than manufacture date.";

                    return null;
                }
            }

            var request =
                new CreateGoodsReceiptRequest
                {
                    PurchaseOrderId =
                        PurchaseOrder
                            .PurchaseOrderId,

                    ReceiptDate =
                        ReceiptDate.Value
                            .UtcDateTime,

                    Notes =
                        Notes,

                    Lines =
                        receiving
                            .Select(x =>
                                new CreateGoodsReceiptLineRequest
                                {
                                    PurchaseOrderLineId =
                                        x.PurchaseOrderLineId,

                                    ReceivingQuantity =
                                        Convert.ToDecimal(
                                            x.ReceivingQuantity),

                                    SupplierBatchNumber =
                                        x.SupplierBatchNumber,

                                    ManufactureDate =
                                        x.ManufactureDate?
                                            .UtcDateTime,

                                    ExpiryDate =
                                        x.ExpiryDate?
                                            .UtcDateTime,

                                    WarrantyMonths =
                                        x.WarrantyMonths > 0
                                            ? Convert.ToInt32(
                                                x.WarrantyMonths)
                                            : null
                                })
                            .ToList()
                };

            return await _goodsReceiptService
                .CreateReceiptAsync(request);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;

            return null;
        }
        finally
        {
            IsSaving = false;
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
            new PropertyChangedEventArgs(
                propertyName));
    }
}