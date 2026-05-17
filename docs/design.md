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
| DoomLite3D | Raylib, Math, **Simulation** (World, View, Kinematics) |
| BouncingBall | Raylib, Math.Arrays, **Simulation** (World.Builders, View), Physics.Collision.Simple |
| WireFishViewer | Avalonia, Transports, Messaging (some still project-ref until packed) |

## CI

Pack dependency repos to an artifact feed, then build `Novolis.Dogfooding.slnx` with `PackageReference` restore.

## Related

- [simulation-layer-policy.md](../../novolis-governance/docs/simulation-layer-policy.md)
- [local-nuget-development.md](../../novolis-governance/docs/local-nuget-development.md)
