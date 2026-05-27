# novolis-dogfooding

Integration workspace that **consumes published Novolis packages** from [GitHub Packages](https://github.com/orgs/Novolis-Platform/packages) (`PackageReference` only).

This repo does not publish packages and has **no GitHub Actions CI**. Library repos validate and publish via their own `merge.yml` / `release.yml` workflows; dogfooding is for local integration against what is already on the feed.

## Quick start

```powershell
git clone https://github.com/Novolis-Platform/novolis-dogfooding.git
cd novolis-dogfooding

# One-time per machine: user NuGet.Config (not repo nuget.config)
..\novolis-governance\scripts\configure-gpr-user-nuget.ps1

dotnet restore
dotnet build --no-restore
dotnet run --project apps/MathGridDemo
```

Feed: `https://nuget.pkg.github.com/Novolis-Platform/index.json` (see `nuget.config`).

Novolis package versions use floating `2026.1.*` in `Directory.Packages.props`. Org setup: [github-packages-org-settings.md](../novolis-governance/docs/github-packages-org-settings.md).

If restore returns 401, re-run `configure-gpr-user-nuget.ps1` (credentials live in `%APPDATA%\NuGet\NuGet.Config`).

## Apps

| App | Folder | Novolis packages exercised |
|-----|--------|---------------------------|
| `MathGridDemo` | `math/` | Math.Arrays |
| `RaylibHello` | `raylib/` | Raylib |
| `XFighter` | `raylib/` | Raylib (3D cockpit demo; moved from `novolis-raylib/samples`) |
| `RaytraceHello` | `rendering/` | Raylib.Game, Rendering (ILGPU + DI + Presentation.Raylib) |
| `SilkTraceHello` | `rendering/` | Rendering (env backend + PathTrace.Demos + Presentation.Silk) |
| `SilkTraceStudio` | `rendering/` | Studio showcase: mouse orbit, backend hotkeys, status strip, glass/emissive scene |
| `DoomLite3D` | `simulation/` | Raylib, Math, Simulation |
| `BouncingBall` | `simulation/` | Raylib, Simulation, Physics.Collision |
| `ArtillerySimulator` | `simulation/` | Raylib, Physics, Simulation |
| `RagdollPlay` | `simulation/` | Raylib, Physics.Joints, Simulation |
| `VoiceSmoke` | `audio/` | Audio.Voice, Voice.Atc (Sherpa Piper TTS, phraseology) |
| `BridgeCommander` | `commands/` | Spectre bridge + voiced patrol exchange; `--interactive` / `--no-voice` |
| `WireFishViewer` | `transports/` | Avalonia, Transports.WireFish, Messaging.Channels |
| `NeuralRacing` | `machine-learning/` | Simulation.Racing + MachineLearning.Neural (evolution demo; glue in app, not a library package) |
