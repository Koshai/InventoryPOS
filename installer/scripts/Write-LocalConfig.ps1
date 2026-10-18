param(
    [Parameter(Mandatory = $true)]
    [string] $ConnectionString,

    [string] $DataRoot = "C:\InventoryPOSData",

    [string] $ConfigDirectory = "$env:ProgramData\InventoryPOS\config"
)

$ErrorActionPreference = "Stop"

New-Item -ItemType Directory -Force -Path $ConfigDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $DataRoot | Out-Null

$configPath = Join-Path $ConfigDirectory "appsettings.Local.json"

$json = @{
    ConnectionStrings = @{
        InventoryPosDatabase = $ConnectionString
    }
    Storage = @{
        DataRoot = $DataRoot
    }
} | ConvertTo-Json -Depth 5

Set-Content -Path $configPath -Value $json -Encoding UTF8

Write-Host "Wrote local config: $configPath"
