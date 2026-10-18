# InventoryPOS licensing release candidate — Windows validation

## Important
This source archive has **not** been compiled or executed in the preparation environment (no .NET SDK/WinUI Windows toolchain). Treat it as a **test candidate**, not a production-ready release. Do not overwrite your only working copy; back up your source and PostgreSQL data first.

## Build and automated tests
Open a PowerShell terminal in the extracted project directory on Windows with the .NET 8 SDK and Windows App SDK build prerequisites installed:

```powershell
dotnet restore .\InventoryPOS.slnx
dotnet build .\InventoryPOS.slnx -c Debug
dotnet test .\tests\InventoryPOS.Tests\InventoryPOS.Tests.csproj -c Debug --logger "console;verbosity=normal"
```

Some existing integration tests require PostgreSQL. Follow the test fixture's configuration before running them. Report all compiler failures and failed test names; do not proceed to release until they are resolved.

## Manual acceptance matrix
Use a test database and a backup. Repeat **each** write operation with (1) a valid license and (2) an expired/invalid/wrong-installation license. For invalid licenses, confirm no database rows, stock movements, payment allocations, or file changes occur.

- Products: create, update, primary image set/remove, barcode add/delete.
- Customers, suppliers, business profile: create/update.
- Inventory: opening stock, adjustments, goods receipt.
- Purchasing: PO, requisition creation/status/conversion, purchase return.
- POS: checkout, sales return, customer payment allocation.
- Sales quotations: create, issue, accept, cancel, expire.
- Warranty: register, inspection, approval/rejection, repair, replacement.
- Document identifiers: create and revoke. Existing lookup must remain readable.
- License activation: valid, tampered signature, wrong installation, expired, missing, unreadable, and replacement license recovery.
- Confirm existing records remain readable after expiration and activation remains accessible.
- Restart app, change system date forward/back, and verify validation behavior. A local-only clock is not a trusted anti-rollback authority.

## Scope and outstanding release work
This patch extends application-service licensing guards; it does **not** certify every workflow, database-level enforcement, concurrent expiry during a transaction, file permissions, or protection against a local administrator modifying binaries.

Installer + License Manager are documented in:
- [VENDOR-LICENSE-AND-INSTALLER.md](VENDOR-LICENSE-AND-INSTALLER.md)
- [CUSTOMER-INSTALL-AND-LICENSE.md](CUSTOMER-INSTALL-AND-LICENSE.md)
- [installer/README.md](../installer/README.md)

Still deferred: code signing of the setup EXE, backup/restore UI, and Postgres major-version upgrades.
