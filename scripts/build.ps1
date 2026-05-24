#Requires -Version 7.0
<#
.SYNOPSIS
  Pack Novolis libraries (if needed), refresh local package restore, and build dogfood apps.
.DESCRIPTION
  RagdollPlay and other apps consume Novolis packages from ../artifacts/nuget-local.
  After library API changes, repack and clear stale global-cache copies of *-local packages
  so restore picks up the new assemblies (same version label, new content).
.PARAMETER Configuration
  MSBuild configuration (Release by default).
.PARAMETER SkipPack
  Skip calling ../scripts/pack-novolis-local.ps1 (use when libraries are already packed).
#>
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$SkipPack
)

$ErrorActionPreference = "Stop"

$DogfoodRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$Solution = Join-Path $DogfoodRoot "Novolis.Dogfooding.slnx"

$prepareArgs = @()
if ($SkipPack) {
    $prepareArgs += "-SkipPack"
}

& (Join-Path $PSScriptRoot "prepare-dogfood-packages.ps1") @prepareArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Building ($Configuration)..." -ForegroundColor Cyan
dotnet build $Solution --no-restore -c $Configuration
exit $LASTEXITCODE
