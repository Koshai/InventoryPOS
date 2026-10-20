# Application updates

InventoryPOS supports two upgrade paths. Both replace **program files only**.

**Preserved on upgrade**
- PostgreSQL database
- `%ProgramData%\InventoryPOS\config\` (connection string)
- `%ProgramData%\InventoryPOS\Licensing\`
- `Storage:DataRoot` (default `C:\InventoryPOSData`)
- Business settings stored in the database

## 1. Upgrade from installer file (available now)

Settings → **Upgrade from installer file…** → select `InventoryPOSSetup.exe`.

The app launches the setup with elevation and exits. Use the same Inno `AppId` so Windows upgrades in place. First-install Postgres provisioning is skipped when `appsettings.Local.json` already exists.

## 2. Check for updates online (license portal feed)

The Desktop client already implements HTTPS feed check, SHA-256 verify, and elevated installer launch.

### Point clients at your portal

1. Deploy the staff license portal ([LICENSE-PORTAL.md](LICENSE-PORTAL.md)).
2. Publish a release from **Releases** (uploads setup + writes `feed.json`).
3. Set `Updates:FeedUrl` on each machine via `%ProgramData%\InventoryPOS\config\appsettings.Local.json` (see [appsettings.Local.updates.example.json](appsettings.Local.updates.example.json)):

```json
{
  "Updates": {
    "FeedUrl": "https://licenses.yourdomain.com/updates/feed.json"
  }
}
```

Or set the same key in the bundled `appsettings.json` for a custom tester build.

4. Settings → **Check for updates online** → **Download and install update**.

Feed shape: [UPDATE-FEED.example.json](UPDATE-FEED.example.json). Until `FeedUrl` is set, the UI explains that online updates are not configured.

## Building a new setup for testers

1. Bump `Version` in `InventoryPOS.Desktop.csproj` and `#define MyAppVersion` in `installer/InventoryPOS.iss`.
2. `powershell -File installer\scripts\Build-Publish.ps1`
3. Compile `installer\InventoryPOS.iss` with Inno Setup.
4. Upload `artifacts\installer\InventoryPOSSetup.exe` in the license portal **Releases** page (preferred), or ship the file manually.
