# Application updates

InventoryPOS supports two upgrade paths. Both replace **program files only**.

**Preserved on upgrade**
- PostgreSQL database
- `%ProgramData%\InventoryPOS\config\` (connection string and optional `Updates:FeedUrl`)
- `%ProgramData%\InventoryPOS\Licensing\`
- `Storage:DataRoot` (default `C:\InventoryPOSData`)
- Business settings stored in the database

## 1. Upgrade from installer file (always available)

Settings → **Upgrade from installer file…** → select `InventoryPOSSetup.exe`.

The app launches the setup with elevation and exits. Use the same Inno `AppId` so Windows upgrades in place. First-install Postgres provisioning is skipped when `appsettings.Local.json` already exists.

## 2. Check for updates online (license portal feed)

The Desktop client implements HTTPS/HTTP feed check, SHA-256 verify, and elevated installer launch.

### Bake the feed into the installer (recommended)

When compiling the setup, set in [`installer/InventoryPOS.iss`](../installer/InventoryPOS.iss):

```iss
#define UpdateFeedUrl "http://YOUR.LINODE.IP/updates/feed.json"
```

First install and later upgrades write/merge that URL into:

`%ProgramData%\InventoryPOS\config\appsettings.Local.json`

See [installer/README.md](../installer/README.md).

### Or set FeedUrl manually

Copy [appsettings.Local.updates.example.json](appsettings.Local.updates.example.json) fields into the machine’s `appsettings.Local.json`, or ship a custom bundled `appsettings.json`.

### Publish a release

1. Deploy the staff license portal ([LICENSE-PORTAL.md](LICENSE-PORTAL.md)).
2. Build `InventoryPOSSetup.exe` (bump version in csproj + ISS).
3. Portal → **Releases** → upload the setup (writes `feed.json` + SHA-256).
4. On the PC: Settings → **Check for updates online** → **Download and install update**.

Feed shape: [UPDATE-FEED.example.json](UPDATE-FEED.example.json). Until `FeedUrl` is set, the UI explains that online updates are not configured.

## Building a new setup for testers

1. Bump `Version` in `InventoryPOS.Desktop.csproj` and `#define MyAppVersion` in `installer/InventoryPOS.iss`.
2. Optionally set `#define UpdateFeedUrl` in the ISS file.
3. `powershell -File installer\scripts\Build-Publish.ps1`
4. Compile `installer\InventoryPOS.iss` with Inno Setup.
5. Upload `artifacts\installer\InventoryPOSSetup.exe` in the license portal **Releases** page (preferred), or ship the file manually.
