# novolis-dogfooding

Integration workspace that **consumes published Novolis packages** from [GitHub Packages](https://github.com/orgs/Novolis-Platform/packages) (`PackageReference` only).

This repo does not publish packages and has **no GitHub Actions CI**. Library repos validate and publish via their own `merge.yml` / `release.yml` workflows; dogfooding is for local integration against what is already on the feed.

Per-app READMEs live under `apps/<name>/README.md` (see also [apps/README.md](apps/README.md) for a short index).

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

**Local iteration:** open `Novolis.Platform.slnx` or pass `-p:NovolisUseProjectReferences=true` when building/running apps against sibling checkouts. Committed consumers use GitHub Packages only.

## Apps

| App | Folder | Novolis packages exercised |
|-----|--------|---------------------------|
| `MathGridDemo` | `apps/MathGridDemo` | Math.Arrays |
| `RaylibHello` | `apps/RaylibHello` | Raylib |
| `HelloGame` … `HelloRaygui` | `apps/raylib/Hello*` | Raylib API walkthroughs |
| `RenderingAvalonia` | `apps/avalonia/RenderingAvalonia` | Avalonia.Rendering + Avalonia.Raylib |
| `MovieMakerLab` | `apps/avalonia/MovieMakerLab` | Video.Edit + Avalonia.Video storyboard |
| `MinimalWorkspaceTimeline` | `apps/workspaces/MinimalWorkspaceTimeline` | Workspaces + Timeline |
| `ProjectTimelineBench` | `apps/workspaces/ProjectTimelineBench` | Workspaces.Projects.Timeline |
| `XFighter` | `apps/raylib/XFighter` | Raylib, Audio (Core, Effects, Playback, Voice) |
| `ArtillerySimulator` | `apps/ArtillerySimulator` | Raylib, Physics.Ballistics, Physics.Collision, Simulation |
| `BouncingBall` | `apps/BouncingBall` | Raylib, Math.Arrays, Simulation, Physics.Collision |
| `DoomLite3D` | `apps/DoomLite3D` | Raylib, Math, Simulation (World, View, Kinematics) |
| `RagdollPlay` | `apps/RagdollPlay` | Raylib, Simulation, Physics.Joints, Physics.Collision |
| `ClothPlay` | `apps/ClothPlay` | Raylib, Simulation, Physics.Joints cloth sheet, Physics.Collision |
| `RandoriFight` | `apps/RandoriFight` | Raylib, Simulation.View, Simulation.Humanoid |
| `PlatformerHop` | `apps/PlatformerHop` | Raylib, Simulation.Kinematics, Simulation.View |
| `PlatformerTwoD` | `apps/PlatformerTwoD` | Rendering.TwoD, Backends.TwoD.Silk, Simulation |
| `RtsLite` | `apps/RtsLite` | Raylib, Simulation (Kinematics, View, World) |
| `RtsLiteTwoD` | `apps/RtsLiteTwoD` | Rendering.TwoD, Backends.TwoD.Silk, Simulation.Kinematics |
| `RaytraceHello` | `apps/rendering/RaytraceHello` | Raylib.Game, Rendering (ILGPU + DI + Presentation.Raylib) |
| `SilkTraceHello` | `apps/rendering/SilkTraceHello` | Rendering (env backend + PathTrace.Demos + Presentation.Silk) |
| `SilkTraceStudio` | `apps/rendering/SilkTraceStudio` | Rendering backends + PathTrace.Demos + Presentation.Silk |
| `SilkTwoDHello` | `apps/rendering/SilkTwoDHello` | Rendering.TwoD, Backends.TwoD.Silk |
| `MeshBench` (Mesh Studio) | `apps/rendering/MeshBench` | Workspaces, Timeline, Snapshots, Rendering, Audio |
| `GamingSmoke` | `apps/gaming/GamingSmoke` | Game.Identity, Game.MenuFlows, Game.Multiplayer.Abstractions |
| `TopDownDoom` | `apps/gaming/TopDownDoom` | Rendering.TwoD, Game flows |
| `TapDuelFootball` | `apps/gaming/TapDuelFootball` | Rendering.TwoD, Game.MenuFlows — hotseat tap duel |
| `NeuralRacing` | `apps/NeuralRacing` | Simulation.Racing, MachineLearning.Neural |
| `VoiceSmoke` | `apps/audio/VoiceSmoke` | Audio.Voice, Voice.Atc (Sherpa Piper TTS) |
| `NovolisVoiceStudio` | `apps/audio/NovolisVoiceStudio` | Voice.Design + Avalonia.Voice |
| `StudioChromeLab` | `apps/avalonia/StudioChromeLab` | Controls dialogs/lists/jobs + Studio focus/dirty chrome |
| `AvaloniaAgentMcp` | `apps/AvaloniaAgentMcp` | Avalonia.Agent.Protocol, Transports.LocalIpc, Agent.Core/Surface |
| `SketchLab` | `apps/avalonia/SketchLab` | SketchControl freehand canvas + PNG/SVG export |
| `ViewportBench` | `apps/avalonia/ViewportBench` | Shared-camera CAD wireframe (OpenGL/CPU/Vulkan/Raylib) |
| `SceneLab` | `apps/avalonia/SceneLab` | Avalonia 3D scene lab |
| `TorrentLab` | `apps/avalonia/TorrentLab` | Avalonia torrent session UI |
| `HumanoidLab` | `apps/avalonia/HumanoidLab` | Simulation.Humanoid, Humanoid.Physics |
| `CharacterLab` | `apps/avalonia/CharacterLab` | Drill/salute rig + character/rifle parade scene |
| `KatoriLab` | `apps/avalonia/KatoriLab` | TSKSR-inspired kenjutsu wire + bokken hold IK |
| `KatoriLab.Tests` | `apps/avalonia/KatoriLab.Tests` | Kata correctness (timeline, holds, walk hang) |
| `FriendLab` | `apps/avalonia/FriendLab` | Find-a-Friend prototype — multi-window users, 3-of-5 interests + geo |
| `CalypsoCad` | `apps/cad/CalypsoCad` | CAD deckplan generation |
| `AstroSmoke` | `apps/astro/AstroSmoke` | Astro catalog/routing/assessment/overlay/plotting |
| `StarMapLab` | `apps/astro/StarMapLab` | Avalonia.StarMap + Astro route planner |
| `EconomyBoard` | `apps/economy/EconomyBoard` | Economy kernel — Avalonia board |
| `TrampFreighterPlay` | `apps/economy/TrampFreighterPlay` | Economy logistics — interactive Spectre |
| `TrampFreighterSim` | `apps/economy/TrampFreighterSim` | Economy logistics — observer Spectre |
| `NearSolPolity` | `apps/economy/NearSolPolity` | Astro catalog bridged to Economy |
| `IoSmoke` | `apps/io/IoSmoke` | IO.Paths, Recovery, Watching, Processes, Git |
| `AdbLab` | `apps/io/AdbLab` | IO.Mobile.Android — ADB protocol |
| `ManuscriptSmoke` | `apps/manuscript/ManuscriptSmoke` | Markup.Manuscript, Voice.Manuscript |
| `BridgeCommander` | `apps/BridgeCommander` | Commands + Audio.Voice (Spectre console) |
| `WireFishViewer` | `apps/WireFishViewer` | Avalonia, Transports.WireFish, Messaging.Channels |

## Shared in-repo libraries

| Library | Folder | Purpose |
|---------|--------|---------|
| `Novolis.Dogfooding.Compose` | `apps/shared/Novolis.Dogfooding.Compose` | ViewPose → rendering camera bridge |
| `Novolis.Dogfooding.TwoD` | `apps/shared/Novolis.Dogfooding.TwoD` | TwoD platform/camera helpers |
| `Novolis.Dogfooding.Voice` | `apps/shared/Novolis.Dogfooding.Voice` | ATC voice DI for demos |
