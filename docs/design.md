# Design

## Purpose

`novolis-dogfooding` validates the Novolis ecosystem **as consumers build it**: library repos publish packages; this repo consumes them via **local or nuget.org feeds** (`PackageReference` only).

## Non-goals

- Publishing NuGet packages from this repository
- Replacing per-repo unit/integration tests
- Cross-repo `ProjectReference` for Novolis libraries (use [local NuGet feed](../../novolis-governance/docs/local-nuget-development.md) instead)

## Local development

Rider **Build Solution** (Ctrl+F9) clears stale `novolis.*` NuGet cache entries via [`Directory.Solution.targets`](../Directory.Solution.targets) → `scripts/prepare-dogfood-packages.ps1 -SkipPack -SkipRestore`, then MSBuild/Rider restore and compile. Keep **Restore NuGet packages before build** enabled in Rider settings. CI skips this hook and packs in its own step.

1. **Full pipeline** (pack + restore + build): `./scripts/build.ps1` or Rider run config **Build Dogfood (pack + restore + build)**
2. After library API changes: run `build.ps1` once, or MSBuild with `-p:DogfoodPrepareArgs=-SkipRestore` (includes pack)
3. Disable the hook: `-p:SkipDogfoodPrepare=true`

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
