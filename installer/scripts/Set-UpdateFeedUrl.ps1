param(
    # Public feed URL from the license portal, e.g.
    # http://203.0.113.10/updates/feed.json
    [Parameter(Mandatory = $true)]
    [string] $UpdateFeedUrl,

    [string] $ConfigDirectory = "$env:ProgramData\InventoryPOS\config"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($UpdateFeedUrl)) {
    Write-Host "UpdateFeedUrl is empty; nothing to write."
    exit 0
}

New-Item -ItemType Directory -Force -Path $ConfigDirectory | Out-Null
$configPath = Join-Path $ConfigDirectory "appsettings.Local.json"

$feed = $UpdateFeedUrl.Trim()

# Build / merge JSON with Windows PowerShell 5.1-compatible objects.
if (Test-Path $configPath) {
    $raw = Get-Content -Path $configPath -Raw -Encoding UTF8
    if ([string]::IsNullOrWhiteSpace($raw)) {
        $raw = "{}"
    }
    $config = $raw | ConvertFrom-Json
}
else {
    $config = [pscustomobject]@{}
}

if ($null -eq $config.Updates) {
    $config | Add-Member -NotePropertyName Updates -NotePropertyValue ([pscustomobject]@{ FeedUrl = $feed }) -Force
}
else {
    if ($null -eq ($config.Updates | Get-Member -Name FeedUrl -ErrorAction SilentlyContinue)) {
        $config.Updates | Add-Member -NotePropertyName FeedUrl -NotePropertyValue $feed -Force
    }
    else {
        $config.Updates.FeedUrl = $feed
    }
}

$json = $config | ConvertTo-Json -Depth 5
Set-Content -Path $configPath -Value $json -Encoding UTF8
Write-Host "Set Updates:FeedUrl in $configPath"
