param(
    [Parameter(Mandatory = $true)]
    [string] $ConnectionString,

    [string] $DataRoot = "C:\InventoryPOSData",

    [string] $ConfigDirectory = "$env:ProgramData\InventoryPOS\config",

    # Optional HTTPS feed from the staff license portal, e.g.
    # https://licenses.yourdomain.com/updates/feed.json
    [string] $UpdateFeedUrl = ""
)

$ErrorActionPreference = "Stop"

New-Item -ItemType Directory -Force -Path $ConfigDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $DataRoot | Out-Null

$configPath = Join-Path $ConfigDirectory "appsettings.Local.json"

$config = @{
    ConnectionStrings = @{
        InventoryPosDatabase = $ConnectionString
    }
    Storage = @{
        DataRoot = $DataRoot
    }
}

if (-not [string]::IsNullOrWhiteSpace($UpdateFeedUrl)) {
    $config.Updates = @{
        FeedUrl = $UpdateFeedUrl.Trim()
    }
}

$json = $config | ConvertTo-Json -Depth 5

Set-Content -Path $configPath -Value $json -Encoding UTF8

Write-Host "Wrote local config: $configPath"
