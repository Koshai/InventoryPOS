# Customer guide — Install and activate InventoryPOS

## Install (one click)

1. Run **InventoryPOSSetup.exe** as Administrator.
2. Follow the wizard (accept defaults).
3. Wait while PostgreSQL and InventoryPOS are configured (first install only).
4. Launch InventoryPOS from the Start Menu or desktop shortcut.

The installer creates the database automatically. You do not need to enter a database password.

## Activate your license

1. Open InventoryPOS → **Licensing**.
2. Click **Copy Installation ID**.
3. Email your vendor:
   - Your **email address**
   - The **Installation ID**
4. When you receive a `.iposlic` file, return to **Licensing** and click **Import license file**.
5. Confirm the status shows **Valid** (or **ExpiringSoon**) and your email.

Writes (sales, stock, etc.) stay blocked until a valid license is imported.

You do **not** log into the vendor’s license portal. The vendor issues the file; you only import it.

## Updates

- **Online** (if your vendor configured it): Settings → **Check for updates online** → download and install when offered.
- **From a file**: Settings → **Upgrade from installer file…** → pick a new `InventoryPOSSetup.exe` your vendor sent.

Upgrades keep your database, license, and ProgramData config.

## Data locations

| Item | Path |
|------|------|
| App | `C:\Program Files\InventoryPOS` |
| Local config | `%ProgramData%\InventoryPOS\config\appsettings.Local.json` |
| Product images / recovery journals | `C:\InventoryPOSData` (default) |
| Activated license | `%ProgramData%\InventoryPOS\Licensing\license.iposlic` |

## Troubleshooting

- **Setup incomplete** dialog: re-run the installer as Administrator, or ask your vendor for support.
- **Wrong installation** after license import: the license was issued for a different PC/Installation ID — request a new license with the current Installation ID.
- **Expired**: ask your vendor to renew (same email + Installation ID).
- **Online updates not configured**: use **Upgrade from installer file…**, or ask your vendor to send a setup that includes their update feed URL.
