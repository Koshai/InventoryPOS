using InventoryPOS.Application.DTOs.Purchasing;
using InventoryPOS.Application.Interfaces;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.ViewModels;

public class GoodsReceiptDetailsViewModel : INotifyPropertyChanged
{
    private readonly IGoodsReceiptService _goodsReceiptService;

    private GoodsReceiptDetailsDto? _receipt;
    private bool _isLoading;
    private string? _errorMessage;

    public GoodsReceiptDetailsDto? Receipt
    {
        get => _receipt;
        private set
        {
            if (_receipt == value)
                return;

            _receipt = value;
            OnPropertyChanged();
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

    public GoodsReceiptDetailsViewModel(
        IGoodsReceiptService goodsReceiptService)
    {
        _goodsReceiptService = goodsReceiptService;
    }

    public async Task LoadAsync(Guid goodsReceiptId)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            Receipt =
                await _goodsReceiptService
                    .GetByIdAsync(goodsReceiptId);

            if (Receipt is null)
            {
                ErrorMessage =
                    "Goods receipt could not be found.";
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

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}