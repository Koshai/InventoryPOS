param(
    [Parameter(Mandatory = $true)]
    [string] $PostgresInstallerPath,

    [string] $InstallDir = "C:\Program Files\PostgreSQL\16",

    [string] $DataRoot = "C:\InventoryPOSData",

    [string] $ServiceAccountPassword = "",

    # Optional license-portal update feed (baked in by InventoryPOS.iss).
    [string] $UpdateFeedUrl = ""
)

$ErrorActionPreference = "Stop"
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

if (-not (Test-Path $PostgresInstallerPath)) {
    throw "PostgreSQL installer not found: $PostgresInstallerPath"
}

function New-StrongPassword {
    $bytes = New-Object byte[] 24
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
    return [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', 'x').Replace('/', 'y')
}

if ([string]::IsNullOrWhiteSpace($ServiceAccountPassword)) {
    $ServiceAccountPassword = New-StrongPassword
}

$secretPath = Join-Path $env:ProgramData "InventoryPOS\config\postgres-super.secret"
$postgresAlreadyInstalled = Test-Path (Join-Path $InstallDir "bin\psql.exe")

if (-not $postgresAlreadyInstalled) {
    Write-Host "Installing PostgreSQL (unattended)..."
    $args = @(
        "--mode", "unattended",
        "--unattendedmodeui", "none",
        "--superpassword", $ServiceAccountPassword,
        "--servicename", "postgresql-x64-16",
        "--servicepassword", $ServiceAccountPassword,
        "--serverport", "5432",
        "--prefix", $InstallDir,
        "--datadir", (Join-Path $InstallDir "data"),
        "--install_runtimes", "0"
    )

    $process = Start-Process -FilePath $PostgresInstallerPath -ArgumentList $args -Wait -PassThru
    if ($process.ExitCode -ne 0) {
        throw "PostgreSQL installer exited with code $($process.ExitCode)."
    }
}
else {
    Write-Host "PostgreSQL already present at $InstallDir"
    if (Test-Path $secretPath) {
        $ServiceAccountPassword = (Get-Content -Path $secretPath -Raw).Trim()
    }
    elseif ([string]::IsNullOrWhiteSpace($ServiceAccountPassword)) {
        throw "PostgreSQL is already installed; pass -ServiceAccountPassword or restore postgres-super.secret."
    }
}

# Wait for server readiness
$psql = Join-Path $InstallDir "bin\psql.exe"
$deadline = (Get-Date).AddMinutes(2)
$ready = $false
$env:PGPASSWORD = $ServiceAccountPassword
while ((Get-Date) -lt $deadline) {
    & $psql -h localhost -p 5432 -U postgres -d postgres -c "SELECT 1;" 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) {
        $ready = $true
        break
    }
    Start-Sleep -Seconds 2
}

if (-not $ready) {
    throw "PostgreSQL did not become ready within the timeout."
}

$connectionString = & (Join-Path $scriptRoot "Provision-Database.ps1") `
    -PostgresBin (Join-Path $InstallDir "bin") `
    -SuperPassword $ServiceAccountPassword

$writeArgs = @{
    ConnectionString = $connectionString
    DataRoot         = $DataRoot
}
if (-not [string]::IsNullOrWhiteSpace($UpdateFeedUrl)) {
    $writeArgs.UpdateFeedUrl = $UpdateFeedUrl
}

& (Join-Path $scriptRoot "Write-LocalConfig.ps1") @writeArgs

# Persist postgres super password for support reinstall only (ACL restricted).
$secretDir = Join-Path $env:ProgramData "InventoryPOS\config"
New-Item -ItemType Directory -Force -Path $secretDir | Out-Null
$secretPath = Join-Path $secretDir "postgres-super.secret"
Set-Content -Path $secretPath -Value $ServiceAccountPassword -Encoding UTF8
icacls $secretPath /inheritance:r | Out-Null
icacls $secretPath /grant:r "Administrators:F" "SYSTEM:F" | Out-Null

Write-Host "PostgreSQL configured. InventoryPOS local config written."
