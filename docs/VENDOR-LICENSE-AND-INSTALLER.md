# Vendor guide — License Manager and installer

## License Manager (day-to-day)

Use **InventoryPOS.LicenseManager** (no CLI required).

```powershell
dotnet run --project tools/InventoryPOS.LicenseManager
```

Or publish and install it on your admin PC.

### First run

1. Click **Generate signing keys** once.
2. Back up `%ProgramData%\InventoryPOS\LicenseIssuer\private-key.pem` offline. Never share it.
3. Copy the matching **public** key PEM into [`TrustedLicenseKeys.cs`](../src/InventoryPOS.Desktop/Licensing/TrustedLicenseKeys.cs) and rebuild POS before shipping to customers.

### Issue or renew a license

1. Customer opens **Licensing** in InventoryPOS and sends you their **email** + **Installation ID**.
2. In License Manager, enter email, display name, Installation ID, and days.
3. Click **Issue / Renew license**.
4. Email the generated `.iposlic` file to the customer (Open folder / Copy path).
5. Customer imports the file on the Licensing page.

Customers are stored in `%ProgramData%\InventoryPOS\LicenseManager\licenses.db` (email, machines, expiry, history).

### Optional CLI

`tools/InventoryPOS.LicenseIssuer` remains for automation; prefer License Manager.

---

## Building the customer installer

See [installer/README.md](../installer/README.md).

Summary:

1. Embed the correct public key in the Desktop app.
2. `Build-Publish.ps1` → `artifacts\publish\win-x64`
3. Place PostgreSQL installer as `installer\redist\postgresql-windows-x64.exe`
4. Compile `installer\InventoryPOS.iss` with Inno Setup

Ship `artifacts\installer\InventoryPOSSetup.exe` to customers.
