; InventoryPOS one-click installer (Inno Setup 6+)
; Prerequisites:
;   1. Download PostgreSQL Windows x64 installer into installer\redist\
;      e.g. postgresql-16.x-windows-x64.exe
;   2. Publish the app:
;      powershell -File installer\scripts\Build-Publish.ps1
;   3. Compile this script with Inno Setup Compiler (ISCC)

#define MyAppName "InventoryPOS"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "InventoryPOS"
#define MyAppExeName "InventoryPOS.Desktop.exe"
#define PublishDir "..\artifacts\publish\win-x64"
#define PostgresRedist "redist\postgresql-windows-x64.exe"

[Setup]
AppId={{5DF30F3B-3234-4097-A585-ED2447A696C2}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\InventoryPOS
DefaultGroupName=InventoryPOS
DisableProgramGroupPage=yes
OutputDir=..\artifacts\installer
OutputBaseFilename=InventoryPOSSetup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
MinVersion=10.0.17763

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"

[Files]
; Published WinUI app (self-contained)
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Installer helper scripts
Source: "scripts\*"; DestDir: "{app}\installer-scripts"; Flags: ignoreversion
; PostgreSQL official installer (must be present as redist\postgresql-windows-x64.exe)
Source: "{#PostgresRedist}"; DestDir: "{tmp}"; DestName: "postgresql-windows-x64.exe"; Flags: deleteafterinstall

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; First install only: create PostgreSQL role/DB and write ProgramData config.
; Upgrades skip this so existing databases, passwords, and preferences are preserved.
Filename: "powershell.exe"; \
  Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\installer-scripts\Install-PostgresAndConfigure.ps1"" -PostgresInstallerPath ""{tmp}\postgresql-windows-x64.exe"" -DataRoot ""C:\InventoryPOSData"""; \
  StatusMsg: "Installing PostgreSQL and creating the InventoryPOS database..."; \
  Flags: runhidden waituntilterminated; \
  Check: not IsExistingInstallation
Filename: "{app}\{#MyAppExeName}"; Description: "Launch InventoryPOS"; Flags: nowait postinstall skipifsilent

[Code]
function IsExistingInstallation: Boolean;
begin
  { Prefer ProgramData local config written by the first-run provisioner. }
  Result := FileExists(ExpandConstant('{commonappdata}\InventoryPOS\config\appsettings.Local.json'));
end;

