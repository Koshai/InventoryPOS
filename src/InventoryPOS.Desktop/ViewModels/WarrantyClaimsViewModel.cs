
using InventoryPOS.Application.DTOs.WarrantyClaims;
using InventoryPOS.Application.Interfaces;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.ViewModels;

public sealed class WarrantyClaimsViewModel
    : INotifyPropertyChanged
{
    private readonly IWarrantyClaimQueryService _service;
    private readonly IWarrantyReplacementService _replacementService;

    private string _searchText = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isLoading;
    private int _currentPage = 1;
    private int _totalPages;
    private int _totalCount;
    private WarrantyClaimDetailsDto? _selectedClaim;
    private readonly IWarrantyClaimManagementService _managementService;

    public WarrantyClaimsViewModel(
    IWarrantyClaimQueryService service,
    IWarrantyClaimManagementService managementService,
    IWarrantyReplacementService replacementService)
    {
        _service = service;
        _managementService = managementService;
        _replacementService = replacementService;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<WarrantyClaimListItemDto> Claims
    { get; } = new();

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value) return;
            _searchText = value;
            Notify();
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            _errorMessage = value;
            Notify();
            Notify(nameof(HasError));
        }
    }

    public bool HasError =>
        !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            _isLoading = value;
            Notify();
            Notify(nameof(CanGoPrevious));
            Notify(nameof(CanGoNext));
            NotifyActionAvailability();
        }
    }

    public int CurrentPage
    {
        get => _currentPage;
        private set
        {
            _currentPage = value;
            Notify();
            Notify(nameof(PageDescription));
            Notify(nameof(CanGoPrevious));
            Notify(nameof(CanGoNext));
        }
    }

    public int TotalPages
    {
        get => _totalPages;
        private set
        {
            _totalPages = value;
            Notify();
            Notify(nameof(PageDescription));
            Notify(nameof(CanGoNext));
        }
    }

    public int TotalCount
    {
        get => _totalCount;
        private set
        {
            _totalCount = value;
            Notify();
            Notify(nameof(CountDescription));
        }
    }

    public string CountDescription =>
        $"{TotalCount} matching claims";

    public string PageDescription =>
        $"Page {CurrentPage} of {Math.Max(1, TotalPages)}";

    public bool CanGoPrevious =>
        !IsLoading && CurrentPage > 1;

    public bool CanGoNext =>
        !IsLoading && CurrentPage < TotalPages;


    private string _inspectionNotesInput = string.Empty;
    private string _decisionNotesInput = string.Empty;

    public string InspectionNotesInput
    {
        get => _inspectionNotesInput;
        set
        {
            if (_inspectionNotesInput == value) return;
            _inspectionNotesInput = value;
            Notify();
        }
    }

    public string DecisionNotesInput
    {
        get => _decisionNotesInput;
        set
        {
            if (_decisionNotesInput == value) return;
            _decisionNotesInput = value;
            Notify();
        }
    }

    public bool CanStartInspection =>
        !IsLoading &&
        SelectedClaim?.Status == "Submitted";

    public bool CanSaveInspection =>
        !IsLoading &&
        SelectedClaim?.Status == "UnderInspection";

    public bool CanDecide =>
        !IsLoading &&
        SelectedClaim?.Status == "UnderInspection" &&
        !string.IsNullOrWhiteSpace(
            SelectedClaim.InspectionNotes);


    public Visibility RepairFormVisibility =>
        SelectedClaim?.Status == "InRepair"
            ? Visibility.Visible
            : Visibility.Collapsed;

    public Visibility CompletedRepairVisibility =>
        SelectedClaim?.Status == "Resolved" &&
        SelectedClaim.Items.Count > 0 &&
        SelectedClaim.Items.All(i => i.ResolutionType == "Repaired")
            ? Visibility.Visible
            : Visibility.Collapsed;

    public string RepairCompletedDate =>
        SelectedClaim?.ResolvedAtUtc is DateTime completed
            ? completed.ToLocalTime().ToString("dd MMM yyyy, hh:mm tt")
            : "—";

    public string FinalRepairNotes =>
        SelectedClaim?.ResolutionNotes ?? "No completion notes recorded.";

    private decimal _replacementQuantity = 1m;
    private string _replacementNotes = string.Empty;

    public decimal ReplacementQuantity
    {
        get => _replacementQuantity;
        set
        {
            if (_replacementQuantity == value)
                return;

            _replacementQuantity = value;
            Notify();
        }
    }

    public string ReplacementNotes
    {
        get => _replacementNotes;
        set
        {
            if (_replacementNotes == value)
                return;

            _replacementNotes = value;
            Notify();
        }
    }

    public Visibility ReplacementFormVisibility =>
        SelectedClaim?.Status == "AwaitingReplacement"
            ? Visibility.Visible
            : Visibility.Collapsed;

    public bool CanIssueReplacement =>
        !IsLoading &&
        SelectedClaim?.Status == "AwaitingReplacement";


    public async Task StartInspectionAsync()
    {
        if (!CanStartInspection || SelectedClaim is null)
            return;

        await RunActionAsync(async id =>
            await _managementService.StartInspectionAsync(id));
    }

    public async Task SaveInspectionAsync()
    {
        if (!CanSaveInspection || SelectedClaim is null)
            return;

        var notes = InspectionNotesInput;

        await RunActionAsync(async id =>
            await _managementService.SaveInspectionAsync(
                new SaveWarrantyInspectionRequest
                {
                    ClaimId = id,
                    InspectionNotes = notes
                }));
    }

    public async Task ApproveAsync()
    {
        if (!CanDecide || SelectedClaim is null)
            return;

        var notes = DecisionNotesInput;

        await RunActionAsync(async id =>
            await _managementService.ApproveAsync(
                new DecideWarrantyClaimRequest
                {
                    ClaimId = id,
                    DecisionNotes = notes
                }));
    }

    public async Task RejectAsync()
    {
        if (!CanDecide || SelectedClaim is null)
            return;

        var notes = DecisionNotesInput;

        if (string.IsNullOrWhiteSpace(notes))
        {
            ErrorMessage = "Enter a rejection reason.";
            return;
        }

        await RunActionAsync(async id =>
            await _managementService.RejectAsync(
                new DecideWarrantyClaimRequest
                {
                    ClaimId = id,
                    DecisionNotes = notes
                }));
    }

    private async Task RunActionAsync(Func<Guid, Task> action)
    {
        if (SelectedClaim is null || IsLoading)
            return;

        var claimId = SelectedClaim.Id;

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            await action(claimId);

            // Refresh details without calling SelectClaimAsync,
            // which would return early while IsLoading is true.
            SelectedClaim =
                await _service.GetDetailsAsync(claimId);

            InspectionNotesInput =
                SelectedClaim?.InspectionNotes ?? string.Empty;

            DecisionNotesInput = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            NotifyActionAvailability();
        }
    }

    private void NotifyActionAvailability()
    {
        Notify(nameof(CanStartInspection));
        Notify(nameof(CanSaveInspection));
        Notify(nameof(CanDecide));
        Notify(nameof(CanStartProcessing));
        Notify(nameof(CanEditTechnicianNotes));
        Notify(nameof(CanCompleteRepair));
        Notify(nameof(CanIssueReplacement));
    }


    public WarrantyClaimDetailsDto? SelectedClaim
    {
        get => _selectedClaim;
        private set
        {
            _selectedClaim = value;
            Notify();
            Notify(nameof(DetailsVisibility));
            Notify(nameof(SelectedClaimNumber));
            Notify(nameof(SelectedClaimStatus));
            Notify(nameof(SelectedInvoiceNumber));
            Notify(nameof(SelectedCustomerName));
            Notify(nameof(SelectedCustomerPhone));
            Notify(nameof(SelectedClaimDate));
            Notify(nameof(SelectedComplaint));
            Notify(nameof(SelectedInspectionNotes));
            Notify(nameof(SelectedResolutionNotes));
            Notify(nameof(SelectedItems));
            Notify(nameof(RepairFormVisibility));
            Notify(nameof(CompletedRepairVisibility));
            Notify(nameof(RepairCompletedDate));
            Notify(nameof(FinalRepairNotes));
            Notify(nameof(ReplacementFormVisibility));
            Notify(nameof(CanIssueReplacement));
            Notify(nameof(CompletedReplacementVisibility));
            Notify(nameof(ReplacementHistory));

            // W6.1 - Warranty audit timeline.
            Notify(nameof(WarrantyTimeline));
            Notify(nameof(HasWarrantyTimeline));
            Notify(nameof(WarrantyTimelineVisibility));
            Notify(nameof(EmptyWarrantyTimelineVisibility));

            NotifyActionAvailability();
        }
    }

    public Visibility DetailsVisibility =>
        SelectedClaim is null
            ? Visibility.Collapsed
            : Visibility.Visible;

    public string SelectedClaimNumber =>
        SelectedClaim?.ClaimNumber ?? string.Empty;

    public string SelectedClaimStatus =>
        SelectedClaim?.Status ?? string.Empty;

    public string SelectedInvoiceNumber =>
        SelectedClaim?.InvoiceNumber ?? string.Empty;

    public string SelectedCustomerName =>
        SelectedClaim?.CustomerName ?? string.Empty;

    public string SelectedCustomerPhone =>
        SelectedClaim?.CustomerPhone ?? "Not provided";

    public string SelectedClaimDate =>
        SelectedClaim?.ClaimDateDisplay ?? string.Empty;

    public string SelectedComplaint =>
        SelectedClaim?.CustomerComplaint ?? "Not provided";

    public string SelectedInspectionNotes =>
        SelectedClaim?.InspectionNotes ?? "Not yet inspected";

    public string SelectedResolutionNotes =>
        SelectedClaim?.ResolutionNotes ?? "Not yet resolved";

    public System.Collections.Generic.IReadOnlyList<
        WarrantyClaimDetailItemDto> SelectedItems =>
            SelectedClaim?.Items
            ?? new System.Collections.Generic.List<
                WarrantyClaimDetailItemDto>();

    public Task LoadAsync() => LoadPageAsync(1);

    public Task SearchAsync() => LoadPageAsync(1);

    public async Task PreviousPageAsync()
    {
        if (CanGoPrevious)
            await LoadPageAsync(CurrentPage - 1);
    }

    public async Task NextPageAsync()
    {
        if (CanGoNext)
            await LoadPageAsync(CurrentPage + 1);
    }

    private async Task LoadPageAsync(int page)
    {
        if (IsLoading) return;

        IsLoading = true;
        ErrorMessage = string.Empty;
        SelectedClaim = null;

        try
        {
            var result = await _service.SearchAsync(
                SearchText, page);

            Claims.Clear();

            foreach (var claim in result.Items)
                Claims.Add(claim);

            CurrentPage = result.Page;
            TotalPages = result.TotalPages;
            TotalCount = result.TotalCount;
        }
        catch (Exception ex)
        {
            ErrorMessage =
                $"Unable to load warranty claims: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task SelectClaimAsync(Guid claimId)
    {
        if (IsLoading) return;

        IsLoading = true;
        ErrorMessage = string.Empty;
        SelectedClaim = null;

        try
        {
            SelectedClaim =
                await _service.GetDetailsAsync(claimId);

            if (SelectedClaim is null)
                ErrorMessage = "The selected claim was not found.";

            InspectionNotesInput = SelectedClaim?.InspectionNotes ?? string.Empty;

            DecisionNotesInput = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage =
                $"Unable to load claim details: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Notify(
        [CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(
            this, new PropertyChangedEventArgs(name));
    }


    public bool CanStartProcessing =>
        !IsLoading &&
        SelectedClaim?.Status == "Approved";

    public bool CanEditTechnicianNotes =>
        !IsLoading &&
        SelectedClaim?.Status == "InRepair";

    public async Task StartRepairAsync(string? processingNotes)
    {
        if (!CanStartProcessing || SelectedClaim is null)
            return;

        await RunActionAsync(id =>
            _managementService.StartRepairAsync(
                new StartWarrantyProcessingRequest
                {
                    ClaimId = id,
                    ProcessingNotes = processingNotes
                }));
    }

    public async Task PrepareReplacementAsync(string? processingNotes)
    {
        if (!CanStartProcessing || SelectedClaim is null)
            return;

        await RunActionAsync(id =>
            _managementService.PrepareReplacementAsync(
                new StartWarrantyProcessingRequest
                {
                    ClaimId = id,
                    ProcessingNotes = processingNotes
                }));
    }

    public async Task SaveTechnicianNotesAsync(
        Guid claimItemId,
        string technicianNotes)
    {
        if (!CanEditTechnicianNotes || SelectedClaim is null)
            return;

        await RunActionAsync(id =>
            _managementService.SaveTechnicianNotesAsync(
                id,
                claimItemId,
                technicianNotes));
    }


    public bool CanCompleteRepair =>
        !IsLoading &&
        SelectedClaim?.Status == "InRepair";

    public async Task CompleteRepairAsync(string completionNotes)
    {
        if (!CanCompleteRepair || SelectedClaim is null)
            return;

        await RunActionAsync(id =>
            _managementService.CompleteRepairAsync(
                new CompleteWarrantyRepairRequest
                {
                    ClaimId = id,
                    CompletionNotes = completionNotes
                }));
    }

    public async Task IssueReplacementAsync(
    Guid claimItemId)
    {
        if (!CanIssueReplacement ||
            SelectedClaim is null)
        {
            return;
        }

        if (claimItemId == Guid.Empty)
        {
            ErrorMessage =
                "Select a valid warranty claim item.";
            return;
        }

        if (ReplacementQuantity <= 0)
        {
            ErrorMessage =
                "Replacement quantity must be greater than zero.";
            return;
        }

        var claimId = SelectedClaim.Id;

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            await _replacementService.IssueAsync(
                new IssueWarrantyReplacementRequest
                {
                    ClaimId = claimId,
                    ClaimItemId = claimItemId,
                    Quantity = ReplacementQuantity,
                    Notes = ReplacementNotes
                });

            SelectedClaim =
                await _service.GetDetailsAsync(claimId);

            ReplacementQuantity = 1m;
            ReplacementNotes = string.Empty;

            // Refresh the list too so its status changes
            // from AwaitingReplacement to Resolved.
            var result = await _service.SearchAsync(
                SearchText,
                CurrentPage);

            Claims.Clear();

            foreach (var claim in result.Items)
                Claims.Add(claim);

            CurrentPage = result.Page;
            TotalPages = result.TotalPages;
            TotalCount = result.TotalCount;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            NotifyActionAvailability();
        }
    }

    public Visibility CompletedReplacementVisibility =>
    SelectedClaim?.Status == "Resolved" &&
    SelectedClaim.Replacements.Count > 0
        ? Visibility.Visible
        : Visibility.Collapsed;

    public IReadOnlyList<WarrantyReplacementHistoryDto>
        ReplacementHistory =>
            SelectedClaim?.Replacements
            ?? new List<WarrantyReplacementHistoryDto>();

    public IReadOnlyList<WarrantyClaimEventDto>
    WarrantyTimeline =>
        SelectedClaim?.Events
        ?? new List<WarrantyClaimEventDto>();

    public bool HasWarrantyTimeline =>
        WarrantyTimeline.Count > 0;

    public Visibility WarrantyTimelineVisibility =>
        HasWarrantyTimeline
            ? Visibility.Visible
            : Visibility.Collapsed;

    public Visibility EmptyWarrantyTimelineVisibility =>
        HasWarrantyTimeline
            ? Visibility.Collapsed
            : Visibility.Visible;
}
