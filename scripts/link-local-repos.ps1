#Requires -Version 7.0
<#
.SYNOPSIS
  Junction sibling Novolis repos from a local monorepo checkout into submodules/ for live dogfooding.
.DESCRIPTION
  Expects this repo at e.g. d:\novolis\novolis-dogfooding with library repos at d:\novolis\novolis-*.
#>
$ErrorActionPreference = "Stop"

$Root = Split-Path $PSScriptRoot -Parent
$MonorepoRoot = (Resolve-Path (Join-Path $Root "..")).Path
$Manifest = Join-Path $Root ".novolis/repos.json"
$config = Get-Content $Manifest -Raw | ConvertFrom-Json
$SubmoduleRoot = Join-Path $Root $config.submodulePath

New-Item -ItemType Directory -Force -Path $SubmoduleRoot | Out-Null

foreach ($repo in $config.repos) {
    $source = Join-Path $MonorepoRoot $repo.name
    $target = Join-Path $SubmoduleRoot $repo.name

    if (-not (Test-Path $source)) {
        Write-Warning "Skip $($repo.name): not found at $source"
        continue
    }

    if (Test-Path $target) {
        $item = Get-Item $target
        if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
            Write-Host "Already linked: $($repo.name)"
            continue
        }
        Write-Error "Target exists and is not a junction: $target"
    }

    Write-Host "Linking $($repo.name) -> $source"
    New-Item -ItemType Junction -Path $target -Target $source | Out-Null
}

Write-Host "Local junctions created. Run: dotnet build Novolis.Dogfooding.slnx"
