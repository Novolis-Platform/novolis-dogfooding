# Downloads Kenney Tiny Dungeon (CC0) and copies a minimal sprite subset into ../Assets/
# Requires: manual download if automated fetch fails (see CREDITS.md)

$ErrorActionPreference = "Stop"
$assetsRoot = Join-Path $PSScriptRoot ".." "Assets"
$zip = Join-Path $env:TEMP "kenney_tiny-dungeon.zip"
$url = "https://kenney.nl/assets/tiny-dungeon"

Write-Host "Kenney Tiny Dungeon: $url"
Write-Host "If download fails, save the zip to: $zip"
Write-Host "Then re-run this script."

if (-not (Test-Path $zip)) {
    try {
        Invoke-WebRequest -Uri $url -OutFile $zip -UseBasicParsing
    }
    catch {
        Write-Warning $_
        exit 1
    }
}

$extract = Join-Path $env:TEMP "kenney_tiny-dungeon"
Expand-Archive -Path $zip -DestinationPath $extract -Force
$pngs = Get-ChildItem -Recurse $extract -Filter "*.png"
Write-Host "Found $($pngs.Count) PNG files under $extract"
Write-Host "Copy suitable enemy/weapon sprites into: $assetsRoot"
