# IoSmoke

Headless dogfood for the core **Novolis.IO.*** packages (from GitHub Packages `2026.1.*`, or ProjectReference mode).

## Exercises

| Package | What runs |
|---------|-----------|
| `Novolis.IO.Paths` | `RootFinder` against dogfooding repo markers |
| `Novolis.IO.Recovery` | Write / get latest / clear snapshots |
| `Novolis.IO.Watching` | Debounced change on a temp file |
| `Novolis.IO.Processes` | Queue `dotnet --version` |
| `Novolis.IO.Git` | `GetStatus` on the dogfooding repo (skipped if no `.git`) |

For Android ADB / APK install, use **AdbLab** instead (`apps/io/AdbLab`).

## Run

```powershell
cd novolis-dogfooding
dotnet restore
dotnet run --project apps/io/IoSmoke

# Local unreleased IO APIs:
dotnet run --project apps/io/IoSmoke -p:NovolisUseProjectReferences=true
```

Exit `0` prints `IoSmoke OK`.
