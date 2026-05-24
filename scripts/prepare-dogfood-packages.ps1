#Requires -Version 7.0
<#
.SYNOPSIS
  Restore dogfood apps from GitHub Packages (Novolis-Platform org feed).
#>
param(
    [switch]$SkipRestore,
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$DogfoodRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$Solution = Join-Path $DogfoodRoot "Novolis.Dogfooding.slnx"
$NuGetConfig = Join-Path $DogfoodRoot "nuget.config"
$ArtifactsDir = Join-Path $DogfoodRoot "artifacts"
$StampFile = Join-Path $ArtifactsDir "dogfood-prepare.stamp"

New-Item -ItemType Directory -Force -Path $ArtifactsDir | Out-Null

if (Test-Path $StampFile) {
    $age = (Get-Date) - (Get-Item $StampFile).LastWriteTime
    if ($age.TotalSeconds -lt 20) {
        Write-Host "Dogfood prepare skipped (ran $([int]$age.TotalSeconds)s ago)." -ForegroundColor DarkGray
        exit 0
    }
}

& (Join-Path $PSScriptRoot "configure-github-packages-auth.ps1") -ConfigFile $NuGetConfig
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (-not $SkipRestore) {
    Write-Host "Restoring $Solution from GitHub Packages..." -ForegroundColor Cyan
    dotnet restore $Solution --configfile $NuGetConfig --force
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
} else {
    Write-Host "Restore skipped (MSBuild/Rider will restore next)." -ForegroundColor DarkGray
}

Set-Content -Path $StampFile -Value (Get-Date).ToString("o") -NoNewline
Write-Host "Dogfood prepare complete." -ForegroundColor DarkGray
