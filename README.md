# novolis-dogfooding

Integration workspace that **consumes published Novolis packages** from [GitHub Packages](https://github.com/orgs/Novolis-Platform/packages) (`PackageReference` only).

This repo does not publish packages. Library repos publish via their `merge.yml` workflows.

## Quick start

```powershell
git clone https://github.com/Novolis-Platform/novolis-dogfooding.git
cd novolis-dogfooding

# PAT with read:packages (or use `gh auth login` and GH_TOKEN)
$env:NOVOLIS_GITHUB_PACKAGES_PAT = "ghp_..."
./scripts/build.ps1
dotnet run --project apps/MathGridDemo
```

Feed: `https://nuget.pkg.github.com/Novolis-Platform/index.json` (see `nuget.config`).

Package versions are pinned in `Directory.Packages.props` (`NovolisPackageVersion`, currently **0.0.1.1**). Bump when library repos publish new builds.

## Scripts

| Script | Purpose |
|--------|---------|
| `scripts/configure-github-packages-auth.ps1` | Wire NuGet credentials for the org feed |
| `scripts/prepare-dogfood-packages.ps1` | Auth + clear `novolis.*` cache + restore |
| `scripts/build.ps1` | Prepare + build solution |

Rider **Build Solution** runs `prepare-dogfood-packages.ps1` via `Directory.Solution.targets` before restore.

## Apps

| App | Novolis packages exercised |
|-----|---------------------------|
| `MathGridDemo` | Math.Arrays |
| `RaylibHello` | Raylib |
| `RaytraceHello` | Raylib, Rendering (CPU + DI), Raylib.Presentation |
| `DoomLite3D` | Raylib, Math, Simulation |
| `BouncingBall` | Raylib, Simulation, Physics.Collision |
| `ArtillerySimulator` | Raylib, Physics, Simulation |
| `RagdollPlay` | Raylib, Physics.Joints, Simulation |
| `BridgeCommander` | Commands |
| `WireFishViewer` | Avalonia, Transports.WireFish, Messaging.Channels |

## Submodules (optional)

`submodules/` and `scripts/sync-submodules.ps1` remain for browsing library **source**; builds do not use them. See `.novolis/repos.json`.

## CI

Push/PR workflows restore from **GitHub Packages** using `GITHUB_TOKEN` (`packages: read`). No local pack step.
