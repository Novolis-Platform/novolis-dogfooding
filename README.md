# novolis-dogfooding

Integration workspace for the Novolis platform: **git submodules** of library repos plus **small executable apps** that reference submodule projects directly (no NuGet round-trip).

This is **not** a package repository. Nothing here is published to NuGet. Use [novolis-template-dotnet](https://github.com/Novolis-Platform/novolis-template-dotnet) for new libraries.

## Layout

| Path | Purpose |
|------|---------|
| `submodules/` | One submodule per library repo (see `.novolis/repos.json`) |
| `apps/` | Small programs that `ProjectReference` into `submodules/` |
| `scripts/` | Submodule sync and local monorepo linking |

## Quick start (local monorepo)

When you already have sibling clones under `d:\novolis\` (or similar):

```powershell
./scripts/link-local-repos.ps1
dotnet build Novolis.Dogfooding.slnx
dotnet run --project apps/MathGridDemo
```

## Quick start (GitHub clones)

```powershell
git clone --recurse-submodules https://github.com/Novolis-Platform/novolis-dogfooding.git
cd novolis-dogfooding
dotnet build Novolis.Dogfooding.slnx
```

Or after a shallow clone:

```powershell
./scripts/sync-submodules.ps1
```

## Apps

| App | Purpose |
|-----|---------|
| `BridgeCommander` | Hex1b TUI dogfood for **Novolis.Commands** (sci-fi bridge captain simulator) |
| `BouncingBall` | Simulation + Raylib physics demo |
| `ArtillerySimulator` | Ballistics + terrain |
| `MathGridDemo` | Math arrays |
| `WireFishViewer` | Packet viewer (Avalonia) |

### Bridge Commander (Novolis.Commands)

```powershell
dotnet run --project apps/BridgeCommander
```

Full-screen TUI: type orders (`helm heading 270`, `tactical fire`, `belay that`), see parse/execute history, switch bridge stations, exercise ambiguity (`fire`). References `../novolis-commands` via project path.

## Adding an app

1. Create `apps/YourApp/YourApp.csproj` with `ProjectReference` paths under `$(NovolisSubmoduleRoot)…` or sibling `novolis-*` repos.
2. Add the project to `Novolis.Dogfooding.slnx`.
3. Open a PR; CI builds all apps with submodules checked out.

## Submodules

Tracked repos are listed in [`.novolis/repos.json`](.novolis/repos.json). Infra repos (`novolis-workflows`, `novolis-governance`, `novolis-registry`, …) are intentionally excluded.

Update all submodules:

```powershell
git submodule update --remote --merge
```

## CI

Push and PR workflows build `Novolis.Dogfooding.slnx` with `submodules: recursive`. There is no release or NuGet publish pipeline.
