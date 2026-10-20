using InventoryPOS.LicensePortal.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InventoryPOS.LicensePortal.Pages.Customers;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public IndexModel(ApplicationDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    public IList<Customer> Customers { get; private set; } = new List<Customer>();

    public async Task OnGetAsync()
    {
        var query = _db.Customers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(Q))
        {
            var term = Q.Trim().ToLower();
            query = query.Where(c =>
                c.Email.ToLower().Contains(term) ||
                c.DisplayName.ToLower().Contains(term));
        }

        Customers = await query
            .OrderBy(c => c.Email)
            .Take(200)
            .ToListAsync();
    }
}
