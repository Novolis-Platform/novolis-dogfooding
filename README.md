# novolis-dogfooding

Integration workspace that **consumes published Novolis packages** from [GitHub Packages](https://github.com/orgs/Novolis-Platform/packages) (`PackageReference` only).

This repo does not publish packages and has **no GitHub Actions CI**. Library repos validate and publish via their own `merge.yml` / `release.yml` workflows; dogfooding is for local integration against what is already on the feed.

## Quick start

```powershell
git clone https://github.com/Novolis-Platform/novolis-dogfooding.git
cd novolis-dogfooding

# One-time: GitHub Packages read access (see governance doc)
gh auth refresh -h github.com -s read:packages

dotnet restore
dotnet build --no-restore
dotnet run --project apps/MathGridDemo
```

Feed: `https://nuget.pkg.github.com/Novolis-Platform/index.json` (see `nuget.config`).

Novolis package versions use floating `2026.1.*` in `Directory.Packages.props`. Org setup: [github-packages-org-settings.md](../novolis-governance/docs/github-packages-org-settings.md).

If restore returns 401, configure the `github` source once:

```powershell
dotnet nuget update source github `
  --username x-access-token `
  --password (gh auth token) `
  --store-password-in-clear-text `
  --configfile nuget.config
```

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
