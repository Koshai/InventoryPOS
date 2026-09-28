using InventoryPOS.Application.DTOs.Purchasing;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.ViewModels;

public class PurchaseRequisitionDetailsViewModel
    : INotifyPropertyChanged
{
    private readonly IPurchaseRequisitionService
        _purchaseRequisitionService;

    private PurchaseRequisitionDetailsDto? _requisition;
    private bool _isLoading;
    private string? _errorMessage;
    private readonly IPurchaseRequisitionPdfService _pdfService;

    public PurchaseRequisitionDetailsViewModel(
        IPurchaseRequisitionService purchaseRequisitionService, IPurchaseRequisitionPdfService pdfService)
    {
        _purchaseRequisitionService =
            purchaseRequisitionService;
        _pdfService = pdfService;
    }

    public PurchaseRequisitionDetailsDto? Requisition
    {
        get => _requisition;
        private set
        {
            if (_requisition == value)
                return;

            _requisition = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(HasRequisition));
            OnPropertyChanged(nameof(RequisitionNumber));
            OnPropertyChanged(nameof(StatusDisplay));
            OnPropertyChanged(nameof(RequisitionDateDisplay));
            OnPropertyChanged(nameof(RequiredByDateDisplay));
            OnPropertyChanged(nameof(ValidUntilDateDisplay));
            OnPropertyChanged(nameof(EstimatedTotalDisplay));
            OnPropertyChanged(nameof(Lines));
            OnPropertyChanged(nameof(CanMarkAsSent));
            OnPropertyChanged(nameof(CanCancel));
            OnPropertyChanged(nameof(CanConvertToPurchaseOrder));
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
            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool CanConvertToPurchaseOrder =>
    Requisition?.Status ==
        PurchaseRequisitionStatus.Sent ||
    Requisition?.Status ==
        PurchaseRequisitionStatus.Approved;

    public bool HasError =>
        !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool HasRequisition =>
        Requisition is not null;

    public string RequisitionNumber =>
        Requisition?.RequisitionNumber ?? string.Empty;

    public string StatusDisplay =>
        Requisition?.Status.ToString() ?? string.Empty;

    public string RequisitionDateDisplay =>
        Requisition is null
            ? string.Empty
            : Requisition.RequisitionDate
                .ToLocalTime()
                .ToString("dd MMM yyyy");

    public string RequiredByDateDisplay =>
        Requisition?.RequiredByDate is DateTime date
            ? date.ToLocalTime().ToString("dd MMM yyyy")
            : "Not specified";

    public string ValidUntilDateDisplay =>
        Requisition?.ValidUntilDate is DateTime date
            ? date.ToLocalTime().ToString("dd MMM yyyy")
            : "Not specified";

    public string EstimatedTotalDisplay =>
        Requisition is null
            ? "৳0.00"
            : $"৳{Requisition.EstimatedTotal:N2}";

    public bool CanMarkAsSent =>
    Requisition?.Status ==
    PurchaseRequisitionStatus.Draft;

    public bool CanCancel =>
        Requisition?.Status ==
            PurchaseRequisitionStatus.Draft ||
        Requisition?.Status ==
            PurchaseRequisitionStatus.Sent;

    public IReadOnlyList<PurchaseRequisitionDetailsLineDto> Lines =>
        Requisition?.Lines ??
        new List<PurchaseRequisitionDetailsLineDto>();

    public async Task LoadAsync(Guid requisitionId)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            Requisition =
                await _purchaseRequisitionService
                    .GetByIdAsync(requisitionId);

            if (Requisition is null)
            {
                ErrorMessage =
                    "Purchase requisition could not be found.";
            }
        }
        catch (Exception ex)
        {
            Requisition = null;
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

    public async Task<byte[]?> GeneratePdfAsync()
    {
        try
        {
            ErrorMessage = null;

            if (Requisition is null)
            {
                ErrorMessage =
                    "No purchase requisition is loaded.";

                return null;
            }

            return await _pdfService
                .GeneratePdfAsync(Requisition);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return null;
        }
    }

    public async Task MarkAsSentAsync()
    {
        if (Requisition is null)
            return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var id =
                Requisition.Id;

            await _purchaseRequisitionService
                .MarkAsSentAsync(id);

            await LoadAsync(id);
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

    public async Task CancelAsync()
    {
        if (Requisition is null)
            return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var id =
                Requisition.Id;

            await _purchaseRequisitionService
                .CancelAsync(id);

            await LoadAsync(id);
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
}