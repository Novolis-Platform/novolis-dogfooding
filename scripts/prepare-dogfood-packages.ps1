#Requires -Version 7.0
<#
.SYNOPSIS
  Pack Novolis libraries (optional), clear stale *-local NuGet cache entries, and restore dogfood apps.
.DESCRIPTION
  Used by Directory.Solution.targets before Rider/MSBuild solution builds and by scripts/build.ps1.
  Does not invoke dotnet build (safe to call from MSBuild BeforeTargets).
.PARAMETER SkipPack
  Skip calling ../scripts/pack-novolis-local.ps1.
.PARAMETER SkipRestore
  Skip dotnet restore (use when MSBuild/Rider will restore immediately after this script).
.PARAMETER Configuration
  Passed to dotnet restore when relevant (currently unused; reserved for future use).
#>
param(
    [switch]$SkipPack,
    [switch]$SkipRestore,
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$DogfoodRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$NovolisRoot = (Resolve-Path (Join-Path $DogfoodRoot "..")).Path
$Solution = Join-Path $DogfoodRoot "Novolis.Dogfooding.slnx"
$NuGetConfig = Join-Path $DogfoodRoot "nuget.config"
$ArtifactsDir = Join-Path $DogfoodRoot "artifacts"
$StampFile = Join-Path $ArtifactsDir "dogfood-prepare.stamp"

New-Item -ItemType Directory -Force -Path $ArtifactsDir | Out-Null

# When MSBuild builds many projects, debounce so prepare runs at most once per wave.
if (Test-Path $StampFile) {
    $age = (Get-Date) - (Get-Item $StampFile).LastWriteTime
    if ($age.TotalSeconds -lt 20) {
        Write-Host "Dogfood prepare skipped (ran $([int]$age.TotalSeconds)s ago)." -ForegroundColor DarkGray
        exit 0
    }
}

if (-not $SkipPack) {
    $packScript = Join-Path $NovolisRoot "scripts\pack-novolis-local.ps1"
    if (-not (Test-Path $packScript)) {
        throw "Pack script not found: $packScript"
    }

    Write-Host "Packing Novolis libraries..." -ForegroundColor Cyan
    & $packScript
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Write-Host "Clearing stale novolis.* packages from global NuGet cache..." -ForegroundColor DarkGray
$globalPackages = Join-Path $env:USERPROFILE ".nuget\packages"
Get-ChildItem -Path $globalPackages -Directory -Filter "novolis.*" -ErrorAction SilentlyContinue |
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "Restoring $Solution..." -ForegroundColor Cyan
if (-not $SkipRestore) {
    dotnet restore $Solution --configfile $NuGetConfig --force
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
} else {
    Write-Host "Restore skipped (MSBuild/Rider will restore next)." -ForegroundColor DarkGray
}

Set-Content -Path $StampFile -Value (Get-Date).ToString("o") -NoNewline
Write-Host "Dogfood package prepare complete." -ForegroundColor DarkGray
