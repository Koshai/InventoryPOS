using InventoryPOS.Application.DTOs.Purchasing;
using InventoryPOS.Application.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.ViewModels;

public class PurchaseReturnViewModel
    : INotifyPropertyChanged
{
    private readonly IPurchaseReturnService
        _purchaseReturnService;

    private bool _isBusy;
    private string? _errorMessage;
    private string? _reason;
    private string? _notes;

    public PurchaseReturnViewModel(
        IPurchaseReturnService purchaseReturnService)
    {
        _purchaseReturnService =
            purchaseReturnService;
    }

    public Guid GoodsReceiptId { get; private set; }

    public string ReceiptNumber { get; private set; }
        = string.Empty;

    public string SupplierName { get; private set; }
        = string.Empty;

    public string LocationName { get; private set; }
        = string.Empty;

    public string PurchaseOrderNumber { get; private set; }
        = string.Empty;

    public DateTime ReceiptDate { get; private set; }

    public ObservableCollection<
        PurchaseReturnLineViewModel> Lines
    {
        get;
    } = new();

    public string? Reason
    {
        get => _reason;

        set
        {
            if (_reason == value)
                return;

            _reason = value;

            OnPropertyChanged();
        }
    }

    public string? Notes
    {
        get => _notes;

        set
        {
            if (_notes == value)
                return;

            _notes = value;

            OnPropertyChanged();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;

        private set
        {
            if (_isBusy == value)
                return;

            _isBusy = value;

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

            OnPropertyChanged(
                nameof(HasError));
        }
    }

    public bool HasError =>
        !string.IsNullOrWhiteSpace(
            ErrorMessage);

    public decimal ReturnTotal =>
        Lines.Sum(x =>
            x.ReturnValue);

    public string ReturnTotalDisplay =>
        $"৳{ReturnTotal:N2}";

    public decimal TotalReturnQuantity =>
        Lines.Sum(x =>
        {
            if (double.IsNaN(
                    x.ReturnQuantity) ||
                double.IsInfinity(
                    x.ReturnQuantity) ||
                x.ReturnQuantity <= 0)
            {
                return 0;
            }

            return Convert.ToDecimal(
                x.ReturnQuantity);
        });

    public async Task LoadAsync(
        Guid goodsReceiptId)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var receipt =
                await _purchaseReturnService
                    .GetReturnableReceiptAsync(
                        goodsReceiptId);

            if (receipt is null)
            {
                ErrorMessage =
                    "Goods receipt could not be found.";

                return;
            }

            GoodsReceiptId =
                receipt.GoodsReceiptId;

            ReceiptNumber =
                receipt.ReceiptNumber;

            ReceiptDate =
                receipt.ReceiptDate;

            SupplierName =
                receipt.SupplierName;

            LocationName =
                receipt.LocationName;

            PurchaseOrderNumber =
                receipt.PurchaseOrderNumber;

            Lines.Clear();

            foreach (var line in receipt.Lines)
            {
                var lineVm =
                    new PurchaseReturnLineViewModel
                    {
                        GoodsReceiptLineId =
                            line.GoodsReceiptLineId,

                        ProductVariantId =
                            line.ProductVariantId,

                        InventoryBatchId =
                            line.InventoryBatchId,

                        ProductName =
                            line.ProductName,

                        SKU =
                            line.SKU,

                        SupplierBatchNumber =
                            line.SupplierBatchNumber,

                        ReceivedQuantity =
                            line.ReceivedQuantity,

                        PreviouslyReturnedQuantity =
                            line.PreviouslyReturnedQuantity,

                        BatchQuantityOnHand =
                            line.BatchQuantityOnHand,

                        MaximumReturnQuantity =
                            line.MaximumReturnQuantity,

                        UnitCost =
                            line.UnitCost,

                        ExpiryDate =
                            line.ExpiryDate,

                        WarrantyMonths =
                            line.WarrantyMonths
                    };

                lineVm.PropertyChanged +=
                    Line_PropertyChanged;

                Lines.Add(lineVm);
            }

            RaiseHeaderProperties();

            RaiseTotals();
        }
        catch (Exception ex)
        {
            ErrorMessage =
                ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<PurchaseReturnResultDto?>
        CompleteAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var selectedLines =
                Lines
                    .Where(x =>
                        !double.IsNaN(
                            x.ReturnQuantity) &&
                        !double.IsInfinity(
                            x.ReturnQuantity) &&
                        x.ReturnQuantity > 0)
                    .ToList();

            if (selectedLines.Count == 0)
            {
                ErrorMessage =
                    "Enter a return quantity for at least one product.";

                return null;
            }

            foreach (var line in selectedLines)
            {
                if (line.ReturnQuantity >
                    line.MaximumReturnQuantityDouble)
                {
                    ErrorMessage =
                        $"{line.ProductName}: return quantity cannot exceed " +
                        $"{line.MaximumReturnQuantity:0.###}.";

                    return null;
                }
            }

            var request =
                new CreatePurchaseReturnRequest
                {
                    GoodsReceiptId =
                        GoodsReceiptId,

                    ReturnDate =
                        DateTime.UtcNow,

                    Reason =
                        Reason,

                    Notes =
                        Notes,

                    Lines =
                        selectedLines
                            .Select(x =>
                                new CreatePurchaseReturnLineRequest
                                {
                                    GoodsReceiptLineId =
                                        x.GoodsReceiptLineId,

                                    InventoryBatchId =
                                        x.InventoryBatchId,

                                    Quantity =
                                        Convert.ToDecimal(
                                            x.ReturnQuantity),

                                    Reason =
                                        x.Reason
                                })
                            .ToList()
                };

            return await _purchaseReturnService
                .CreateReturnAsync(request);
        }
        catch (Exception ex)
        {
            ErrorMessage =
                ex.Message;

            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Line_PropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName ==
                nameof(
                    PurchaseReturnLineViewModel
                        .ReturnQuantity))
        {
            RaiseTotals();
        }
    }

    private void RaiseTotals()
    {
        OnPropertyChanged(
            nameof(ReturnTotal));

        OnPropertyChanged(
            nameof(ReturnTotalDisplay));

        OnPropertyChanged(
            nameof(TotalReturnQuantity));
    }

    private void RaiseHeaderProperties()
    {
        OnPropertyChanged(
            nameof(GoodsReceiptId));

        OnPropertyChanged(
            nameof(ReceiptNumber));

        OnPropertyChanged(
            nameof(ReceiptDate));

        OnPropertyChanged(
            nameof(SupplierName));

        OnPropertyChanged(
            nameof(LocationName));

        OnPropertyChanged(
            nameof(PurchaseOrderNumber));
    }

    public event PropertyChangedEventHandler?
        PropertyChanged;

    protected virtual void OnPropertyChanged(
        [CallerMemberName]
        string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(
                propertyName));
    }
}