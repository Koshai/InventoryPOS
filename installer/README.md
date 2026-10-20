# InventoryPOS one-click installer

Builds `InventoryPOSSetup.exe`, which:

1. Installs the published WinUI app under `C:\Program Files\InventoryPOS`
2. Silently installs PostgreSQL 16 (from `redist\`) on **first install only**
3. Creates database/user with a random password (first install)
4. Writes `%ProgramData%\InventoryPOS\config\appsettings.Local.json`
5. Creates `C:\InventoryPOSData` for images and recovery journals
6. Optionally writes `Updates:FeedUrl` when you bake a license-portal URL into the ISS script (first install **and** upgrade)

Upgrades (same `AppId`) replace program files only and skip Postgres provisioning when `appsettings.Local.json` already exists.

## Prerequisites (build machine)

- .NET 8 SDK
- Windows App SDK build tools
- [Inno Setup 6](https://jrsoftware.org/isinfo.php)
- PostgreSQL Windows x64 installer in `installer\redist\postgresql-windows-x64.exe`

## Build steps

1. Bump version in both places (keep in sync):
   - `src/InventoryPOS.Desktop/InventoryPOS.Desktop.csproj` (`Version` / `InformationalVersion`)
   - `installer/InventoryPOS.iss` (`#define MyAppVersion`)

2. **Optional — online updates:** edit `installer/InventoryPOS.iss` and set your portal feed:

```iss
#define UpdateFeedUrl "http://YOUR.LINODE.IP/updates/feed.json"
```

Leave it as `""` if you are not using the license portal feed yet.

3. Publish and compile:

```powershell
# From repo root
powershell -ExecutionPolicy Bypass -File installer\scripts\Build-Publish.ps1

& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\InventoryPOS.iss
```

Output: `artifacts\installer\InventoryPOSSetup.exe`

4. Upload that setup to the license portal **Releases** page (so Settings → Check for updates can find it), and/or ship the file to customers.

## Customer machine

Run `InventoryPOSSetup.exe` as Administrator → Finish → Launch InventoryPOS.

First launch applies EF migrations and seeds defaults. Activate a license under **Licensing** (email + Installation ID → vendor sends `.iposlic`).

If `UpdateFeedUrl` was baked in, Settings → **Check for updates online** uses the portal. Otherwise use **Upgrade from installer file…**.

## Related docs

- [Customer install & license](../docs/CUSTOMER-INSTALL-AND-LICENSE.md)
- [Vendor license portal & installer](../docs/VENDOR-LICENSE-AND-INSTALLER.md)
- [Application updates](../docs/APPLICATION-UPDATES.md)
- [License portal (VPS)](../docs/LICENSE-PORTAL.md)
