param(
    [string] $Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

$desktopProject = "src\InventoryPOS.Desktop\InventoryPOS.Desktop.csproj"
$publishDir = Join-Path $repoRoot "artifacts\publish\win-x64"

Write-Host "Publishing InventoryPOS.Desktop (win-x64 self-contained unpackaged)..."
dotnet publish $desktopProject `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishReadyToRun=true `
    -p:PublishTrimmed=false `
    -p:WindowsPackageType=None `
    -p:WindowsAppSDKSelfContained=true `
    -o $publishDir

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed."
}

Write-Host "Published to $publishDir"
Write-Host "Next:"
Write-Host "  1. Place PostgreSQL installer at installer\redist\postgresql-windows-x64.exe"
Write-Host "  2. Optional: set #define UpdateFeedUrl in installer\InventoryPOS.iss (license portal feed)"
Write-Host "  3. Compile installer\InventoryPOS.iss with Inno Setup"
Write-Host "  4. Upload artifacts\installer\InventoryPOSSetup.exe to the license portal Releases page"
