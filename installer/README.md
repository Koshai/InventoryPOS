# InventoryPOS one-click installer

Builds `InventoryPOSSetup.exe`, which:

1. Installs the published WinUI app under `C:\Program Files\InventoryPOS`
2. Silently installs PostgreSQL 16 (from `redist\`)
3. Creates database/user with a random password
4. Writes `%ProgramData%\InventoryPOS\config\appsettings.Local.json`
5. Creates `C:\InventoryPOSData` for images and recovery journals

## Prerequisites (build machine)

- .NET 8 SDK
- Windows App SDK build tools
- [Inno Setup 6](https://jrsoftware.org/isinfo.php)
- PostgreSQL Windows x64 installer in `installer\redist\postgresql-windows-x64.exe`

## Build steps

```powershell
# From repo root
powershell -ExecutionPolicy Bypass -File installer\scripts\Build-Publish.ps1

# Then compile with Inno Setup (ISCC on PATH or via GUI)
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\InventoryPOS.iss
```

Output: `artifacts\installer\InventoryPOSSetup.exe`

## Customer machine

Run `InventoryPOSSetup.exe` as Administrator → Finish → Launch InventoryPOS.

First launch applies EF migrations and seeds defaults. Activate a license under **Licensing**.
