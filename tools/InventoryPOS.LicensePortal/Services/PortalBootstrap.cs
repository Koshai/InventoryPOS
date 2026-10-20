using InventoryPOS.LicensePortal.Data;
using InventoryPOS.LicensePortal.Options;
using InventoryPOS.Licensing.Issuer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InventoryPOS.LicensePortal.Services;

public static class PortalBootstrap
{
    public const string RoleAdmin = "Admin";
    public const string RoleIssuer = "Issuer";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("PortalBootstrap");
        var options = sp.GetRequiredService<IOptions<PortalOptions>>().Value;
        var db = sp.GetRequiredService<ApplicationDbContext>();
        var userManager = sp.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var keys = sp.GetRequiredService<LicenseKeyStore>();
        var releases = sp.GetRequiredService<ReleasePublishService>();

        Directory.CreateDirectory(options.KeyDirectory);
        Directory.CreateDirectory(options.IssuedLicensesDirectory);
        Directory.CreateDirectory(options.ReleasesDirectory);

        await db.Database.MigrateAsync();

        foreach (var role in new[] { RoleAdmin, RoleIssuer })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        if (!await userManager.Users.AnyAsync())
        {
            if (string.IsNullOrWhiteSpace(options.BootstrapAdminPassword))
            {
                logger.LogWarning(
                    "No users exist and Portal:BootstrapAdminPassword is empty. " +
                    "Set the password env var and restart to create the first admin.");
            }
            else
            {
                var admin = new IdentityUser
                {
                    UserName = options.BootstrapAdminEmail,
                    Email = options.BootstrapAdminEmail,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(admin, options.BootstrapAdminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRolesAsync(admin, new[] { RoleAdmin, RoleIssuer });
                    logger.LogInformation(
                        "Bootstrap admin created for {Email}. Change the password after first login.",
                        options.BootstrapAdminEmail);
                }
                else
                {
                    logger.LogError(
                        "Failed to create bootstrap admin: {Errors}",
                        string.Join("; ", result.Errors.Select(e => e.Description)));
                }
            }
        }

        if (!keys.HasKeys)
        {
            logger.LogWarning(
                "Signing keys not found under {Dir}. Mount private-key.pem and public-key.pem before issuing licenses.",
                options.KeyDirectory);
        }
        else
        {
            logger.LogInformation("Signing keys found under {Dir}.", options.KeyDirectory);
        }

        await releases.WriteFeedFileAsync();
    }
}
