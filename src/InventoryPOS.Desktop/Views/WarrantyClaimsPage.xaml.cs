
using InventoryPOS.Application.DTOs.WarrantyClaims;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Desktop.ViewModels;
using InventoryPOS.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.System;

namespace InventoryPOS.Desktop.Views;

public sealed partial class WarrantyClaimsPage : Page
{
    public WarrantyClaimsViewModel ViewModel { get; }

    private readonly IWarrantyClaimPdfService
    _warrantyClaimPdfService;

    private readonly IPdfPrintService
        _pdfPrintService;

    public WarrantyClaimsPage()
    {
        ViewModel = App.Services
            .GetRequiredService<WarrantyClaimsViewModel>();

        _warrantyClaimPdfService =
            App.Services.GetRequiredService<
                IWarrantyClaimPdfService>();

        _pdfPrintService =
            App.Services.GetRequiredService<
                IPdfPrintService>();

        InitializeComponent();
        DataContext = ViewModel;
    }

    protected override async void OnNavigatedTo(
    NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        await ViewModel.LoadAsync();

        if (e.Parameter is Guid claimId &&
            claimId != Guid.Empty)
        {
            await ViewModel.SelectClaimAsync(
                claimId);
        }
    }

    private async void Search_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.SearchAsync();
    }

    private async void ClaimSearchBox_KeyDown(
        object sender,
        KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Enter)
            return;

        e.Handled = true;
        await ViewModel.SearchAsync();
    }

    private async void ClaimResults_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (ClaimResultsList.SelectedItem
            is WarrantyClaimListItemDto claim)
        {
            await ViewModel.SelectClaimAsync(claim.Id);
        }
    }

    private async void PreviousPage_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.PreviousPageAsync();
    }

    private async void NextPage_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.NextPageAsync();
    }


    private async void StartInspection_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.StartInspectionAsync();
    }

    private async void SaveInspection_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.SaveInspectionAsync();
    }

    private async void ApproveClaim_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.ApproveAsync();
    }

    private async void RejectClaim_Click(
        object sender,
        RoutedEventArgs e)
    {
        var confirmation = new ContentDialog
        {
            XamlRoot = this.XamlRoot,
            Title = "Reject Warranty Claim",
            Content = "Reject this claim and release its reserved warranty quantity?",
            PrimaryButtonText = "Reject Claim",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close
        };

        if (await confirmation.ShowAsync()
            == ContentDialogResult.Primary)
        {
            await ViewModel.RejectAsync();
        }
    }


    private async void StartRepair_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!ViewModel.CanStartProcessing)
            return;

        await ViewModel.StartRepairAsync(
            ProcessingNotesBox.Text);

        if (ViewModel.SelectedClaim?.Status == "InRepair")
            ProcessingNotesBox.Text = string.Empty;
    }

    private async void PrepareReplacement_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!ViewModel.CanStartProcessing)
            return;

        var confirmation = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Prepare Replacement",
            Content =
                "Move this claim to Awaiting Replacement? " +
                "No stock will be deducted yet.",
            PrimaryButtonText = "Continue",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close
        };

        if (await confirmation.ShowAsync()
            != ContentDialogResult.Primary)
        {
            return;
        }

        await ViewModel.PrepareReplacementAsync(
            ProcessingNotesBox.Text);

        if (ViewModel.SelectedClaim?.Status == "AwaitingReplacement")
            ProcessingNotesBox.Text = string.Empty;
    }

    private async void EditTechnicianNotes_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!ViewModel.CanEditTechnicianNotes)
            return;

        if (sender is not Button button ||
            button.DataContext
                is not WarrantyClaimDetailItemDto item)
        {
            return;
        }

        var notesBox = new TextBox
        {
            Header = "Technician notes",
            PlaceholderText =
                "Describe the repair work or technical findings...",
            Text = item.TechnicianNotes ?? string.Empty,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 140,
            MaxLength = 2000
        };

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = $"Technician Notes — {item.ProductName}",
            Content = notesBox,
            PrimaryButtonText = "Save Notes",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary
        };

        // Validate before closing the dialog.
        dialog.PrimaryButtonClick += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(notesBox.Text))
                return;

            args.Cancel = true;

            notesBox.PlaceholderText =
                "Technician notes are required.";
        };

        if (await dialog.ShowAsync()
            != ContentDialogResult.Primary)
        {
            return;
        }

        await ViewModel.SaveTechnicianNotesAsync(
            item.Id,
            notesBox.Text);
    }


    private async void CompleteRepair_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!ViewModel.CanCompleteRepair)
            return;

        var notes = RepairCompletionNotesBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(notes))
        {
            var warning = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = "Completion Notes Required",
                Content = "Enter the final repair findings before completing this claim.",
                CloseButtonText = "OK"
            };

            await warning.ShowAsync();
            return;
        }

        var confirmation = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Complete Warranty Repair",
            Content =
                "Mark every item in this claim as repaired " +
                "and close the warranty claim?",
            PrimaryButtonText = "Complete Repair",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close
        };

        if (await confirmation.ShowAsync()
            != ContentDialogResult.Primary)
        {
            return;
        }

        await ViewModel.CompleteRepairAsync(notes);

        if (ViewModel.SelectedClaim?.Status == "Resolved")
            RepairCompletionNotesBox.Text = string.Empty;
    }

    private async void IssueReplacement_Click(
    object sender,
    RoutedEventArgs e)
    {
        if (!ViewModel.CanIssueReplacement)
            return;

        if (sender is not Button button ||
            button.DataContext
                is not WarrantyClaimDetailItemDto item)
        {
            return;
        }

        var quantity = ViewModel.ReplacementQuantity;

        if (quantity <= 0)
            return;

        var confirmation = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Issue Warranty Replacement",
            Content =
                $"Issue {quantity:N3} replacement unit(s) " +
                $"for {item.ProductName}?\n\n" +
                "Inventory will be deducted immediately.",
            PrimaryButtonText = "Issue Replacement",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close
        };

        if (await confirmation.ShowAsync()
            != ContentDialogResult.Primary)
        {
            return;
        }

        await ViewModel.IssueReplacementAsync(item.Id);
    }

    // =========================================================
    // WARRANTY CLAIM PDF
    // =========================================================

    private async void ExportWarrantyPdf_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (ViewModel.SelectedClaim is null)
            return;

        try
        {
            var picker =
                new Windows.Storage.Pickers
                    .FileSavePicker();

            var hwnd =
                WinRT.Interop.WindowNative
                    .GetWindowHandle(
                        App.Services
                            .GetRequiredService<
                                MainWindow>());

            WinRT.Interop.InitializeWithWindow
                .Initialize(
                    picker,
                    hwnd);

            picker.SuggestedStartLocation =
                Windows.Storage.Pickers
                    .PickerLocationId
                    .DocumentsLibrary;

            picker.FileTypeChoices.Add(
                "PDF Document",
                new List<string>
                {
                ".pdf"
                });

            picker.SuggestedFileName =
                $"WarrantyClaim-" +
                $"{ViewModel.SelectedClaim.ClaimNumber}";

            var file =
                await picker.PickSaveFileAsync();

            if (file is null)
                return;

            var pdf =
                await _warrantyClaimPdfService
                    .GenerateWarrantyClaimPdfAsync(
                        ViewModel.SelectedClaim);

            await Windows.Storage.FileIO
                .WriteBytesAsync(
                    file,
                    pdf);
        }
        catch (Exception ex)
        {
            await ShowPdfErrorDialogAsync(
                "PDF Export Failed",
                ex.Message);
        }
    }

    private async void PrintWarrantyPdf_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (ViewModel.SelectedClaim is null)
            return;

        try
        {
            var pdf =
                await _warrantyClaimPdfService
                    .GenerateWarrantyClaimPdfAsync(
                        ViewModel.SelectedClaim);

            await _pdfPrintService.PrintAsync(
                pdf,
                $"WarrantyClaim-" +
                $"{ViewModel.SelectedClaim.ClaimNumber}");
        }
        catch (Exception ex)
        {
            await ShowPdfErrorDialogAsync(
                "Print Failed",
                ex.Message);
        }
    }

    private async Task ShowPdfErrorDialogAsync(
        string title,
        string message)
    {
        var dialog =
            new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = XamlRoot
            };

        await dialog.ShowAsync();
    }
}
