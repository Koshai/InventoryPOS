param(
    [string] $PostgresBin = "C:\Program Files\PostgreSQL\16\bin",
    [string] $SuperUser = "postgres",
    [Parameter(Mandatory = $true)]
    [string] $SuperPassword,
    [string] $AppUser = "inventorypos",
    [string] $DatabaseName = "inventorypos",
    [int] $Port = 5432
)

$ErrorActionPreference = "Stop"

$psql = Join-Path $PostgresBin "psql.exe"
if (-not (Test-Path $psql)) {
    throw "psql.exe not found at $psql. Install PostgreSQL first."
}

function New-StrongPassword {
    $bytes = New-Object byte[] 24
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
    return [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', 'A').Replace('/', 'B')
}

$appPassword = New-StrongPassword
$env:PGPASSWORD = $SuperPassword

function Invoke-Psql([string] $Sql) {
    & $psql -h localhost -p $Port -U $SuperUser -d postgres -v ON_ERROR_STOP=1 -c $Sql
    if ($LASTEXITCODE -ne 0) {
        throw "psql failed for: $Sql"
    }
}

# Role + database (idempotent-ish for reinstalls)
$roleExists = & $psql -h localhost -p $Port -U $SuperUser -d postgres -tAc "SELECT 1 FROM pg_roles WHERE rolname = '$AppUser'"
if ($roleExists -ne "1") {
    Invoke-Psql "CREATE ROLE $AppUser LOGIN PASSWORD '$appPassword';"
}
else {
    Invoke-Psql "ALTER ROLE $AppUser WITH LOGIN PASSWORD '$appPassword';"
}

$dbExists = & $psql -h localhost -p $Port -U $SuperUser -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname = '$DatabaseName'"
if ($dbExists -ne "1") {
    Invoke-Psql "CREATE DATABASE $DatabaseName OWNER $AppUser;"
}

Invoke-Psql "GRANT ALL PRIVILEGES ON DATABASE $DatabaseName TO $AppUser;"

$connectionString = "Host=localhost;Port=$Port;Database=$DatabaseName;Username=$AppUser;Password=$appPassword"

Write-Output $connectionString
