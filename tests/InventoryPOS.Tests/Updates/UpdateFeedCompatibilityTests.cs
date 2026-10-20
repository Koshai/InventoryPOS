using System.Text.Json;

namespace InventoryPOS.Tests.Updates;

/// <summary>
/// Ensures portal feed JSON (camelCase) matches Desktop AppUpdateService parsing.
/// </summary>
public sealed class UpdateFeedCompatibilityTests
{
    private sealed class UpdateFeedDocument
    {
        public string? Version { get; set; }
        public string? DownloadUrl { get; set; }
        public string? Sha256 { get; set; }
        public string? Notes { get; set; }
    }

    [Fact]
    public void Portal_feed_json_deserializes_like_desktop()
    {
        const string json = """
            {
              "version": "1.2.0",
              "downloadUrl": "https://licenses.example.com/updates/InventoryPOSSetup-1.2.0.exe",
              "sha256": "abc123",
              "notes": "Test"
            }
            """;

        var feed = JsonSerializer.Deserialize<UpdateFeedDocument>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(feed);
        Assert.Equal("1.2.0", feed!.Version);
        Assert.Equal(
            "https://licenses.example.com/updates/InventoryPOSSetup-1.2.0.exe",
            feed.DownloadUrl);
        Assert.Equal("abc123", feed.Sha256);
        Assert.Equal("Test", feed.Notes);
    }
}
