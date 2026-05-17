# MCP validation for Bridge Commander.
# Raw stdio framing is awkward in PowerShell; this delegates to the in-process MCP tool suite.
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent

Push-Location $root
try {
    if (-not (Test-Path "bin\Release\net10.0\BridgeCommander.dll")) {
        dotnet build -c Release
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }

    dotnet run -c Release --no-build -- --mcp-test
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
