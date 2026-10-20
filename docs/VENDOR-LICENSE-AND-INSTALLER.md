# Vendor guide — License portal, License Manager, and installer

## License portal (day-to-day, recommended)

Staff-only web dashboard on your VPS: issue licenses and publish desktop updates.

See **[LICENSE-PORTAL.md](LICENSE-PORTAL.md)** and [`deploy/license-portal/`](../deploy/license-portal/).

1. Customer sends **email** + **Installation ID**.
2. Portal → **Issue license** → download `.iposlic` → email to customer.
3. Portal → **Releases** → upload `InventoryPOSSetup.exe` so Settings → Check for updates works when `Updates:FeedUrl` is set.

## License Manager (offline / emergency)

Use **InventoryPOS.LicenseManager** when the portal is unavailable (air-gapped keys).

```powershell
dotnet run --project tools/InventoryPOS.LicenseManager
```

### First run (keys — same key pair for portal and Manager)

1. Click **Generate signing keys** once (or copy existing PEMs to the portal `data/keys` mount).
2. Back up `%ProgramData%\InventoryPOS\LicenseIssuer\private-key.pem` offline. Never share it.
3. Copy the matching **public** key PEM into [`TrustedLicenseKeys.cs`](../src/InventoryPOS.Application/Licensing/TrustedLicenseKeys.cs) and rebuild POS before shipping to customers.

### Issue or renew (Manager)

1. Customer opens **Licensing** in InventoryPOS and sends you their **email** + **Installation ID**.
2. Enter email, display name, Installation ID, and days → **Issue / Renew license**.
3. Email the `.iposlic` file; customer imports it on the Licensing page.

Local CRM: `%ProgramData%\InventoryPOS\LicenseManager\licenses.db`.

### Optional CLI

`tools/InventoryPOS.LicenseIssuer` remains for automation; prefer the portal or License Manager.

---

## Building the customer installer

See [installer/README.md](../installer/README.md).

Summary:

1. Embed the correct public key in the Desktop app (`TrustedLicenseKeys`).
2. Bump version in Desktop csproj **and** `#define MyAppVersion` in `installer/InventoryPOS.iss`.
3. Optional online updates: set `#define UpdateFeedUrl "http://YOUR.PORTAL.IP/updates/feed.json"` in the ISS file (empty = file-only upgrades).
4. `Build-Publish.ps1` → `artifacts\publish\win-x64`
5. Place PostgreSQL installer as `installer\redist\postgresql-windows-x64.exe`
6. Compile `installer\InventoryPOS.iss` with Inno Setup
7. Upload `artifacts\installer\InventoryPOSSetup.exe` to the portal **Releases** page (and/or email to customers)

The installer writes `Updates:FeedUrl` into ProgramData on first install and on upgrade when `UpdateFeedUrl` is set.
