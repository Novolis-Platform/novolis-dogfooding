#Requires -Version 7.0
<#
.SYNOPSIS
  Configure GitHub Packages credentials for the org NuGet feed.
.DESCRIPTION
  Uses GITHUB_TOKEN in Actions (no org secret) when packages grant this repo access.
  Locally uses gh OAuth after: gh auth refresh -h github.com -s read:packages
  Optional: NOVOLIS_GPR_TOKEN or NOVOLIS_GITHUB_PACKAGES_PAT
#>
param(
    [string]$ConfigFile = (Join-Path $PSScriptRoot "..\nuget.config")
)

$ErrorActionPreference = "Stop"
$ConfigFile = (Resolve-Path $ConfigFile).Path
$source = "https://nuget.pkg.github.com/Novolis-Platform/index.json"
$inActions = $env:GITHUB_ACTIONS -eq 'true' -or $env:CI -eq 'true'

function Test-GhLoggedIn {
    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) { return $false }
    return (gh auth status 2>&1 | Out-String) -match 'Logged in to github\.com'
}

function Get-GprToken {
    if ($inActions -and $env:GITHUB_TOKEN) { return $env:GITHUB_TOKEN }
    foreach ($name in @('NOVOLIS_GPR_TOKEN', 'NOVOLIS_GITHUB_PACKAGES_PAT', 'GITHUB_TOKEN', 'GH_TOKEN')) {
        $value = [Environment]::GetEnvironmentVariable($name)
        if ($value) { return $value }
    }
    if (Test-GhLoggedIn) {
        $gh = gh auth token 2>$null
        if ($gh) { return $gh.Trim() }
    }
    return $null
}

$token = Get-GprToken
if (-not $token) {
    $msg = @"
GitHub Packages (NuGet) requires authentication even for public packages.
  Actions: ensure org packages grant 'novolis-dogfooding' Actions access (see novolis-governance/docs/github-packages-org-settings.md).
  Local:   gh auth refresh -h github.com -s read:packages
"@
    Write-Error $msg
}

# Prefer gh / env tokens with packages scope (read:packages or write:packages).
dotnet nuget remove source github --configfile $ConfigFile 2>$null | Out-Null
dotnet nuget add source $source `
    --name github `
    --username "x-access-token" `
    --password $token `
    --store-password-in-clear-text `
    --configfile $ConfigFile | Out-Null

Write-Host "GitHub Packages NuGet source configured." -ForegroundColor DarkGray
