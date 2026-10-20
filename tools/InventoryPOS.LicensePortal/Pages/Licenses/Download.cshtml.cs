using InventoryPOS.LicensePortal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InventoryPOS.LicensePortal.Pages.Licenses;

public class DownloadModel : PageModel
{
    private readonly LicenseIssueService _issueService;

    public DownloadModel(LicenseIssueService issueService)
    {
        _issueService = issueService;
    }

    public string? FileName { get; private set; }
    public string? ErrorMessage { get; private set; }

    public IActionResult OnGet(string fileName)
    {
        FileName = Path.GetFileName(fileName);
        try
        {
            _ = _issueService.GetIssuedLicensePath(FileName);
            return Page();
        }
        catch (FileNotFoundException)
        {
            ErrorMessage = "License file not found.";
            return Page();
        }
    }

    public IActionResult OnGetFile(string fileName)
    {
        try
        {
            var path = _issueService.GetIssuedLicensePath(fileName);
            return PhysicalFile(path, "application/json", Path.GetFileName(path));
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
    }
}
