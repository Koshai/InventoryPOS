using InventoryPOS.LicensePortal.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InventoryPOS.LicensePortal.Pages.Customers;

public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public DetailsModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public Customer? Customer { get; private set; }
    public IList<MachineLicense> Machines { get; private set; } = new List<MachineLicense>();
    public IList<IssueHistory> History { get; private set; } = new List<IssueHistory>();

    [BindProperty(SupportsGet = true)]
    public long? MachineId { get; set; }

    public async Task<IActionResult> OnGetAsync(long id)
    {
        Customer = await _db.Customers.FindAsync(id);
        if (Customer is null)
            return NotFound();

        Machines = await _db.MachineLicenses
            .Where(m => m.CustomerId == id)
            .OrderByDescending(m => m.ExpiresAtUtc)
            .ToListAsync();

        if (MachineId is null && Machines.Count > 0)
            MachineId = Machines[0].Id;

        if (MachineId is long mid)
        {
            History = await _db.IssueHistories
                .Where(h => h.MachineLicenseId == mid)
                .OrderByDescending(h => h.IssuedAtUtc)
                .ToListAsync();
        }

        return Page();
    }
}
