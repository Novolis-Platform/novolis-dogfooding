# novolis-dogfooding

Integration workspace that **consumes published Novolis packages** from [GitHub Packages](https://github.com/orgs/Novolis-Platform/packages) (`PackageReference` only).

This repo does not publish packages and has **no GitHub Actions CI**. Library repos validate and publish via their own `merge.yml` / `release.yml` workflows; dogfooding is for local integration against what is already on the feed.

## Quick start

```powershell
git clone https://github.com/Novolis-Platform/novolis-dogfooding.git
cd novolis-dogfooding

# One-time: grant gh read:packages (no manual PAT if org packages are configured — see governance doc)
gh auth refresh -h github.com -s read:packages
./scripts/build.ps1
dotnet run --project apps/MathGridDemo
```

Feed: `https://nuget.pkg.github.com/Novolis-Platform/index.json` (see `nuget.config`).

Novolis package versions use floating `*` in `Directory.Packages.props` (latest from GitHub Packages). Org setup: [github-packages-org-settings.md](../novolis-governance/docs/github-packages-org-settings.md).

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
