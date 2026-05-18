# Design

## Purpose

`novolis-dogfooding` validates the Novolis ecosystem **as consumers build it**: library repos publish packages; this repo consumes them via **local or nuget.org feeds** (`PackageReference` only).

## Non-goals

- Publishing NuGet packages from this repository
- Replacing per-repo unit/integration tests
- Cross-repo `ProjectReference` for Novolis libraries (use [local NuGet feed](../../novolis-governance/docs/local-nuget-development.md) instead)

## Local development

1. Pack libraries: `d:\novolis\scripts\pack-novolis-local.ps1`
2. Restore using [`nuget.config`](../nuget.config) (`novolis-local` → `../artifacts/nuget-local`)
3. `dotnet build Novolis.Dogfooding.slnx`

Optional: `submodules/` junctions via `scripts/link-local-repos.ps1` for **source browsing** only.

## Apps

| App | Packages exercised |
|-----|-------------------|
| MathGridDemo | `Novolis.Math.Arrays` |
| RaylibHello | `Novolis.Raylib` |
| DoomLite3D | Raylib, Math, Simulation.World + View + Kinematics (PackageReference) |
| BouncingBall | Raylib, Math.Arrays, Simulation.World + World.Builders, Physics.Collision.Simple (PackageReference) |
| ArtillerySimulator | Raylib, Physics.Ballistics, Physics.Collision.Simple, Simulation.World + World.Builders + View (PackageReference) |
| RagdollPlay | Raylib, Physics.Collision.Simple + **Physics.Joints**, Simulation.World + World.Builders |
| PlatformerHop | Raylib, Simulation.Kinematics + View (side-view tile platformer) |
| RtsLite | Raylib, Simulation.View; fixed C&amp;C-style camera; CC0 building billboards; infinite-ore build menu |
| RandoriFight | Raylib, Simulation.View; katana randori (men/kesa/tsuki, chūdan kamae), Tekken-plane 3D, side camera, AI |
| BridgeCommander | **Novolis.Commands** (Engine, Queueing, Abstractions) via sibling project ref; Hex1b TUI |
| WireFishViewer | Avalonia, Transports, Messaging (some still project-ref until packed) |

## CI

Pack dependency repos to an artifact feed, then build `Novolis.Dogfooding.slnx` with `PackageReference` restore.

## Related

- [simulation-layer-policy.md](../../novolis-governance/docs/simulation-layer-policy.md)
- [local-nuget-development.md](../../novolis-governance/docs/local-nuget-development.md)
