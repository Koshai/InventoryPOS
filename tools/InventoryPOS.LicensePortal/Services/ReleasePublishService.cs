using System.Security.Cryptography;
using System.Text.Json;
using InventoryPOS.LicensePortal.Data;
using InventoryPOS.LicensePortal.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InventoryPOS.LicensePortal.Services;

public sealed class ReleasePublishService
{
    private static readonly JsonSerializerOptions FeedJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ApplicationDbContext _db;
    private readonly PortalOptions _options;

    public ReleasePublishService(ApplicationDbContext db, IOptions<PortalOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<AppRelease> PublishAsync(
        string version,
        Stream content,
        string notes,
        string publishedByUserName,
        bool setAsCurrent,
        CancellationToken cancellationToken = default)
    {
        version = version.Trim();
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("Version is required.", nameof(version));

        Directory.CreateDirectory(_options.ReleasesDirectory);

        var fileName = $"InventoryPOSSetup-{version}.exe";
        var path = Path.Combine(_options.ReleasesDirectory, fileName);

        await using (var file = File.Create(path))
        {
            await content.CopyToAsync(file, cancellationToken);
        }

        var sha256 = await ComputeSha256HexAsync(path, cancellationToken);

        var existing = await _db.AppReleases
            .FirstOrDefaultAsync(r => r.Version == version, cancellationToken);

        if (existing is not null)
        {
            if (!string.Equals(existing.FileName, fileName, StringComparison.OrdinalIgnoreCase)
                && File.Exists(Path.Combine(_options.ReleasesDirectory, existing.FileName)))
            {
                File.Delete(Path.Combine(_options.ReleasesDirectory, existing.FileName));
            }

            existing.FileName = fileName;
            existing.Sha256 = sha256;
            existing.Notes = notes ?? string.Empty;
            existing.PublishedAtUtc = DateTimeOffset.UtcNow;
            existing.PublishedByUserName = publishedByUserName;
            if (setAsCurrent)
                existing.IsCurrent = true;
        }
        else
        {
            existing = new AppRelease
            {
                Version = version,
                FileName = fileName,
                Sha256 = sha256,
                Notes = notes ?? string.Empty,
                IsCurrent = setAsCurrent,
                PublishedAtUtc = DateTimeOffset.UtcNow,
                PublishedByUserName = publishedByUserName
            };
            _db.AppReleases.Add(existing);
        }

        if (setAsCurrent)
        {
            var others = await _db.AppReleases
                .Where(r => r.Version != version && r.IsCurrent)
                .ToListAsync(cancellationToken);
            foreach (var other in others)
                other.IsCurrent = false;
            existing.IsCurrent = true;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await WriteFeedFileAsync(cancellationToken);
        return existing;
    }

    public async Task WriteFeedFileAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_options.ReleasesDirectory);

        var current = await _db.AppReleases
            .Where(r => r.IsCurrent)
            .OrderByDescending(r => r.PublishedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        current ??= await _db.AppReleases
            .OrderByDescending(r => r.PublishedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var feedPath = Path.Combine(_options.ReleasesDirectory, "feed.json");
        if (current is null)
        {
            if (File.Exists(feedPath))
                File.Delete(feedPath);
            return;
        }

        var baseUrl = _options.PublicBaseUrl.TrimEnd('/');
        var feed = new
        {
            version = current.Version,
            downloadUrl = $"{baseUrl}/updates/{current.FileName}",
            sha256 = current.Sha256,
            notes = current.Notes
        };

        await File.WriteAllTextAsync(
            feedPath,
            JsonSerializer.Serialize(feed, FeedJsonOptions),
            cancellationToken);
    }

    public string GetReleasePath(string fileName)
    {
        var safe = Path.GetFileName(fileName);
        var path = Path.Combine(_options.ReleasesDirectory, safe);
        if (!File.Exists(path))
            throw new FileNotFoundException("Release file not found.", path);
        return path;
    }

    public string GetFeedPath()
    {
        var path = Path.Combine(_options.ReleasesDirectory, "feed.json");
        if (!File.Exists(path))
            throw new FileNotFoundException("Update feed not found. Publish a release first.", path);
        return path;
    }

    private static async Task<string> ComputeSha256HexAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
