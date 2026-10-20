using InventoryPOS.LicensePortal.Data;
using InventoryPOS.LicensePortal.Options;
using InventoryPOS.LicensePortal.Services;
using InventoryPOS.Licensing.Issuer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<PortalOptions>(
    builder.Configuration.GetSection(PortalOptions.SectionName));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 1024L * 1024L * 1024L; // 1 GB installer uploads
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1024L * 1024L * 1024L;
});

var portalOptions = builder.Configuration
    .GetSection(PortalOptions.SectionName)
    .Get<PortalOptions>() ?? new PortalOptions();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 10;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Staff", policy =>
        policy.RequireRole(PortalBootstrap.RoleAdmin, PortalBootstrap.RoleIssuer));
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole(PortalBootstrap.RoleAdmin));
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Customers", "Staff");
    options.Conventions.AuthorizeFolder("/Licenses", "Staff");
    options.Conventions.AuthorizeFolder("/Releases", "Staff");
    options.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
});

builder.Services.AddSingleton(_ => new LicenseKeyStore(portalOptions.KeyDirectory));
builder.Services.AddSingleton(sp =>
{
    var keys = sp.GetRequiredService<LicenseKeyStore>();
    return new LicenseIssuerService(keys);
});
builder.Services.AddScoped<LicenseIssueService>();
builder.Services.AddScoped<ReleasePublishService>();

var app = builder.Build();

await PortalBootstrap.InitializeAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseForwardedHeaders();
// TLS terminates at Caddy in production; only redirect in Development.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/updates/feed.json", (ReleasePublishService releases) =>
{
    try
    {
        var path = releases.GetFeedPath();
        return Results.File(path, "application/json");
    }
    catch (FileNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
});

app.MapGet("/updates/{fileName}", (string fileName, ReleasePublishService releases) =>
{
    if (string.Equals(fileName, "feed.json", StringComparison.OrdinalIgnoreCase))
    {
        try
        {
            return Results.File(releases.GetFeedPath(), "application/json");
        }
        catch (FileNotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
    }

    try
    {
        var path = releases.GetReleasePath(fileName);
        return Results.File(
            path,
            "application/octet-stream",
            fileDownloadName: Path.GetFileName(path));
    }
    catch (FileNotFoundException)
    {
        return Results.NotFound();
    }
});

app.MapRazorPages();

app.Run();
