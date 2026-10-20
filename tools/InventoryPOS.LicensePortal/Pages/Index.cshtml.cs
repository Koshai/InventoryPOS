using InventoryPOS.LicensePortal.Options;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace InventoryPOS.LicensePortal.Pages;

public class IndexModel : PageModel
{
    private readonly PortalOptions _options;

    public IndexModel(IOptions<PortalOptions> options)
    {
        _options = options.Value;
    }

    public string FeedUrl => $"{_options.PublicBaseUrl.TrimEnd('/')}/updates/feed.json";

    public void OnGet()
    {
    }
}
