# Dogfood apps

Small executables that consume **published Novolis packages** from GitHub Packages (`PackageReference` in each `.csproj`).

Add a project under `apps/` (or `apps/<repo>/` for multi-app repos like `rendering/`), declare packages in `Directory.Packages.props`, and register it in `Novolis.Dogfooding.slnx` under the solution folder for the **primary** Novolis repo it dogfoods (`/raylib/`, `/rendering/`, `/simulation/`, …).

In-repo API walkthroughs (`HelloGame`, `HelloRuntime`, …) stay in `novolis-raylib/samples/`. Published-package demos and cross-repo integration apps live here.

## WireFish Viewer

Live packet capture UI for `Novolis.Transports.WireFish` (WireShark-style layout). Requires **Npcap** on Windows for capture devices.

```bash
dotnet run --project apps/WireFishViewer
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

## DoomLite3D

```bash
dotnet run --project apps/DoomLite3D
```
