# Dogfood apps

Small executables that consume **published Novolis packages** from GitHub Packages (`PackageReference` in each `.csproj`).

Add a project under `apps/` (or `apps/<repo>/` for multi-app repos like `rendering/`), declare packages in `Directory.Packages.props`, and register it in `Novolis.Dogfooding.slnx` under the solution folder for the **primary** Novolis repo it dogfoods (`/raylib/`, `/rendering/`, `/simulation/`, …).

API walkthroughs (`HelloGame`, `HelloRuntime`, …) live under `apps/raylib/Hello*`. Library repos keep packable `src/`, tests, and `tools/` only — no `samples/` or product `apps/`.

## FriendLab

Multi-window Find-a-Friend prototype (3-of-5 interest overlap + geo radius). Control window opens one Avalonia window per simulated app user.

```bash
dotnet run --project apps/avalonia/FriendLab
```

## SketchLab

Freehand sketch dogfood for `Novolis.Avalonia.Controls` `SketchControl`. Clipboard export only (transparent PNG or SVG text).

```bash
dotnet run --project apps/avalonia/SketchLab -p:NovolisUseProjectReferences=true
```

## ViewportBench

Same CAD wireframe on OpenGL / CPU / Vulkan / Raylib with one shared orbit camera and present-time HUD (idle + orbit motion).

```bash
dotnet run --project apps/avalonia/ViewportBench -p:NovolisUseProjectReferences=true
dotnet run --project apps/avalonia/ViewportBench -p:NovolisUseProjectReferences=true -- --lights
```

## Calypso CAD

Hand-ported Rev G deckplans → `.cadjson` / `.cadshapejson` / `.cadlayers.json`, with two-sided walls and interior views.

```bash
dotnet run --project apps/cad/CalypsoCad
dotnet run --project apps/cad/CalypsoCad -- --generate-only
```

## Calypso Internals CAD

CAL-INT lock + manufacturer hull → Novolis CAD companions + Wavefront OBJ (optional Raylib orbit view).

```powershell
dotnet run --project d:\novolis\novolis-dogfooding\apps\cad\CalypsoInternalsCad\CalypsoInternalsCad.csproj -p:NovolisUseProjectReferences=true
dotnet run --project d:\novolis\novolis-dogfooding\apps\cad\CalypsoInternalsCad\CalypsoInternalsCad.csproj -p:NovolisUseProjectReferences=true -- --view
```

## FreightWing

X-Wing Alliance–inspired dual-role campaign (freighter → X-wing transfer). Bake content from a local Steam install via the **local-only** `novolis-experimental` `Xwa.Cli` tree (not on GitHub), then run the app (no Steam/experimental at runtime).

```powershell
$env:XWA_INSTALL_DIR = "D:\Steam\steamapps\common\Star Wars X-Wing Alliance"
dotnet run --project d:\novolis\novolis-experimental\src\Novolis.Experimental.Xwa.Cli -- all --out d:\novolis\novolis-dogfooding\apps\raylib\FreightWing\Content
dotnet run --project d:\novolis\novolis-dogfooding\apps\raylib\FreightWing -p:NovolisUseProjectReferences=true
```

## SilkTwoDHello

Orthographic 2D sample (`Rendering.TwoD` + Silk): platforms, `TwoDCollisionWorld`, HUD, menus.

```bash
dotnet run --project apps/rendering/SilkTwoDHello
```

## PlatformerTwoD

Same tile demo as PlatformerHop, but **planar XZ** via `PlanarAgent` and **Silk TwoD** drawing (pairs with Raylib `PlatformerHop`).

```bash
dotnet run --project apps/PlatformerTwoD
```

## RtsLiteTwoD

Top-down RTS on **orthographic TwoD** (shared sim with `RtsLite`; sand field + tiberium patches, tank markers). **Mouse:** LMB select, RMB orders. Classic **diagonal RA camera + sprites:** `RtsLite` (Raylib).

```bash
dotnet run --project apps/RtsLiteTwoD
```

## RtsLite (Raylib)

Pseudo-3D C&amp;C-style camera + building sprites — kept for Raylib/billboard dogfood.

```bash
dotnet run --project apps/RtsLite
```

## IoSmoke / AdbLab

```bash
dotnet run --project apps/io/IoSmoke
dotnet run --project apps/io/AdbLab -p:NovolisUseProjectReferences=true
dotnet run --project apps/io/AdbLab -p:NovolisUseProjectReferences=true -- --smoke
```

- `IoSmoke` — Paths, Recovery, Watching, Processes, Git (`apps/io/IoSmoke/README.md`)
- `AdbLab` — Mobile.Android protocol/stats/install (`apps/io/AdbLab/README.md`)

## Tap Duel Football

Portrait hotseat tap-tug football (`Rendering.TwoD` + `Game.MenuFlows`), recreation of [tap-duel-football](https://github.com/frankhaugen/tap-duel-football).

```powershell
dotnet run --project apps/gaming/TapDuelFootball -p:NovolisUseProjectReferences=true
```

## PulseStrip

Anti-grav spline-circuit racer (Wipeout homage): weapons/boost, procedural FX/SFX, evolutionary ML opponents. Windows + Linux; Android deferred (see app README).

```powershell
dotnet run --project d:\novolis\novolis-dogfooding\apps\raylib\PulseStrip -p:NovolisUseProjectReferences=true
dotnet run --project d:\novolis\novolis-dogfooding\apps\raylib\PulseStrip -p:NovolisUseProjectReferences=true -- --smoke
```
