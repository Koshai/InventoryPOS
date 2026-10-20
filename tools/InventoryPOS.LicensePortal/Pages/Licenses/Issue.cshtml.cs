using System.ComponentModel.DataAnnotations;
using InventoryPOS.LicensePortal.Services;
using InventoryPOS.Licensing.Issuer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InventoryPOS.LicensePortal.Pages.Licenses;

public class IssueModel : PageModel
{
    private readonly LicenseIssueService _issueService;
    private readonly LicenseKeyStore _keys;

    public IssueModel(LicenseIssueService issueService, LicenseKeyStore keys)
    {
        _issueService = issueService;
        _keys = keys;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }
    public bool HasKeys => _keys.HasKeys;

    public class InputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string DisplayName { get; set; } = string.Empty;

        [Required, StringLength(64)]
        [Display(Name = "Installation ID")]
        public string InstallationId { get; set; } = string.Empty;

        [Range(1, 3650)]
        public int Days { get; set; } = 90;

        [StringLength(2000)]
        public string? Notes { get; set; }
    }

    public void OnGet(string? email, string? name, string? installationId)
    {
        if (!string.IsNullOrWhiteSpace(email))
            Input.Email = email;
        if (!string.IsNullOrWhiteSpace(name))
            Input.DisplayName = name;
        if (!string.IsNullOrWhiteSpace(installationId))
            Input.InstallationId = installationId;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!HasKeys)
        {
            ErrorMessage = "Signing keys are not mounted. Place private-key.pem and public-key.pem in the key directory.";
            return Page();
        }

        if (!ModelState.IsValid)
            return Page();

        try
        {
            var userName = User.Identity?.Name ?? "unknown";
            var (_, path) = await _issueService.IssueOrRenewAsync(
                Input.Email,
                Input.DisplayName,
                Input.InstallationId,
                Input.Days,
                userName,
                Input.Notes,
                cancellationToken);

            return RedirectToPage("Download", new { fileName = Path.GetFileName(path) });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
    }
}
