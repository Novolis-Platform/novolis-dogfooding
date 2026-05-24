#Requires -Version 7.0
<#
.SYNOPSIS
  Add credentials for the Novolis-Platform GitHub Packages NuGet feed.
.DESCRIPTION
  Uses GITHUB_TOKEN (Actions), NOVOLIS_GITHUB_PACKAGES_PAT, or GH_TOKEN.
  Username: GITHUB_ACTOR, NOVOLIS_GITHUB_PACKAGES_USER, or `gh api user`.
#>
param(
    [string]$ConfigFile = (Join-Path $PSScriptRoot "..\nuget.config")
)

$ErrorActionPreference = "Stop"
$ConfigFile = (Resolve-Path $ConfigFile).Path

$token = $env:GITHUB_TOKEN
if (-not $token) { $token = $env:NOVOLIS_GITHUB_PACKAGES_PAT }
if (-not $token) { $token = $env:GH_TOKEN }
if (-not $token) {
    Write-Warning "No GitHub token (GITHUB_TOKEN / NOVOLIS_GITHUB_PACKAGES_PAT / GH_TOKEN). Restore from GitHub Packages may fail."
    exit 0
}

$user = $env:GITHUB_ACTOR
if (-not $user) { $user = $env:NOVOLIS_GITHUB_PACKAGES_USER }
if (-not $user -and (Get-Command gh -ErrorAction SilentlyContinue)) {
    $user = gh api user -q .login 2>$null
}
if (-not $user) { $user = "token" }

$source = "https://nuget.pkg.github.com/Novolis-Platform/index.json"
$sources = dotnet nuget list source --configfile $ConfigFile 2>$null
if ($sources -match 'github') {
    dotnet nuget update source github `
        --username $user `
        --password $token `
        --store-password-in-clear-text `
        --configfile $ConfigFile | Out-Null
} else {
    dotnet nuget add source $source `
        --name github `
        --username $user `
        --password $token `
        --store-password-in-clear-text `
        --configfile $ConfigFile | Out-Null
}

Write-Host "GitHub Packages NuGet source configured (user: $user)." -ForegroundColor DarkGray
