# Dogfood apps

Small executables that consume **published Novolis packages** from GitHub Packages (`PackageReference` in each `.csproj`).

Add a project under `apps/` (or `apps/<repo>/` for multi-app repos like `rendering/`), declare packages in `Directory.Packages.props`, and register it in `Novolis.Dogfooding.slnx` under the solution folder for the **primary** Novolis repo it dogfoods (`/raylib/`, `/rendering/`, `/simulation/`, …).

In-repo API walkthroughs (`HelloGame`, `HelloRuntime`, …) stay in `novolis-raylib/samples/`. Published-package demos and cross-repo integration apps live here.

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

## DoomLite3D

```bash
dotnet run --project apps/DoomLite3D
```
