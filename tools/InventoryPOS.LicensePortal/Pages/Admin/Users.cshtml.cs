using System.ComponentModel.DataAnnotations;
using InventoryPOS.LicensePortal.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InventoryPOS.LicensePortal.Pages.Admin;

public class UsersModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;

    public UsersModel(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    public IList<StaffUserRow> Users { get; private set; } = new List<StaffUserRow>();
    public string? StatusMessage { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public InviteInput Input { get; set; } = new();

    public sealed class StaffUserRow
    {
        public string Email { get; set; } = string.Empty;
        public string Roles { get; set; } = string.Empty;
    }

    public class InviteInput
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 10)]
        [DataType(DataType.Password)]
        public string TemporaryPassword { get; set; } = string.Empty;

        public bool IsAdmin { get; set; }
    }

    public async Task OnGetAsync()
    {
        await LoadUsersAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadUsersAsync();

        if (!ModelState.IsValid)
            return Page();

        var existing = await _userManager.FindByEmailAsync(Input.Email);
        if (existing is not null)
        {
            ErrorMessage = "A user with that email already exists.";
            return Page();
        }

        var user = new IdentityUser
        {
            UserName = Input.Email.Trim(),
            Email = Input.Email.Trim(),
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, Input.TemporaryPassword);
        if (!result.Succeeded)
        {
            ErrorMessage = string.Join("; ", result.Errors.Select(e => e.Description));
            return Page();
        }

        var roles = new List<string> { PortalBootstrap.RoleIssuer };
        if (Input.IsAdmin)
            roles.Add(PortalBootstrap.RoleAdmin);

        await _userManager.AddToRolesAsync(user, roles);
        StatusMessage = $"Created staff user {user.Email}. They should change the password after login.";
        Input = new InviteInput();
        await LoadUsersAsync();
        return Page();
    }

    private async Task LoadUsersAsync()
    {
        var list = new List<StaffUserRow>();
        foreach (var user in await _userManager.Users.OrderBy(u => u.Email).ToListAsync())
        {
            var roles = await _userManager.GetRolesAsync(user);
            list.Add(new StaffUserRow
            {
                Email = user.Email ?? user.UserName ?? "",
                Roles = string.Join(", ", roles.OrderBy(r => r))
            });
        }

        Users = list;
    }
}
