#Requires -Version 7.0
$ErrorActionPreference = "Stop"

$Root = Split-Path $PSScriptRoot -Parent
$Manifest = Join-Path $Root ".novolis/repos.json"
$config = Get-Content $Manifest -Raw | ConvertFrom-Json
$SubmoduleRoot = Join-Path $Root $config.submodulePath

New-Item -ItemType Directory -Force -Path $SubmoduleRoot | Out-Null

foreach ($repo in $config.repos) {
    $path = Join-Path $SubmoduleRoot $repo.name
    $url = "https://github.com/$($config.organization)/$($repo.name).git"

    if (Test-Path (Join-Path $path ".git")) {
        Write-Host "Updating $($repo.name)..."
        git -C $path fetch origin
        git -C $path checkout main 2>$null
        if ($LASTEXITCODE -ne 0) { git -C $path checkout master }
        git -C $path pull --ff-only
        continue
    }

    if (Test-Path $path) {
        Write-Error "Path exists but is not a git repo: $path"
    }

    Write-Host "Cloning $($repo.name)..."
    git clone --depth 1 --branch main $url $path 2>$null
    if ($LASTEXITCODE -ne 0) {
        git clone --depth 1 $url $path
    }
}

Write-Host "Submodules ready under $SubmoduleRoot"
