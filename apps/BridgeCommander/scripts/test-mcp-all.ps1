# Full MCP + QA test battery for Bridge Commander.
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent

Push-Location $root
try {
    dotnet build -c Release
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    dotnet run -c Release --no-build -- --mcp-test
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    dotnet run -c Release --no-build -- --qa-smoke
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host "All Bridge Commander MCP/QA tests passed."
}
finally {
    Pop-Location
}
