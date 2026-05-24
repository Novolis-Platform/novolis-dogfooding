#Requires -Version 7.0
<#
.SYNOPSIS
  Restore from GitHub Packages and build dogfood apps.
#>
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$DogfoodRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$Solution = Join-Path $DogfoodRoot "Novolis.Dogfooding.slnx"

& (Join-Path $PSScriptRoot "prepare-dogfood-packages.ps1")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Building ($Configuration)..." -ForegroundColor Cyan
dotnet build $Solution --no-restore -c $Configuration
exit $LASTEXITCODE
