# Calypso CAD

Dogfood app that hand-ports the **Calypso** transport (RevE / RevG + Chapter 16 canon) into `novolis.cad` companions and renders plan / orbit / interior exploration.

## Canon lock

| Spec | Value |
|------|-------|
| Hull | **65 m LOA · 20 m beam · 12 m height** |
| Hold | **22 × 19 × 9 m** claim · geometry **~18 × 19 × 9** (STN 47→AP) · HILS-C40 **5×1×3** |
| Engineering | **Full OAH (~12 m)** continuous machinery void (STN ≈ 38.8–47) |
| Decks | **−1 utilities · 0 ops/crew · +1 passengers** (hab stack fwd/mid only) |
| WT bulkheads | `wt-bh-eng` @ STN **38.8** · `wt-bh-hold` @ STN **47.0** |
| Corridors | Twin port/stbd + cross · ClearWidth **2.0 m** · T-junctions = vestibules · PD ≥ **1.0 m** |
| Registry | ST-7749-63325116 |
| Source | `calypso-deckplans_revG.svg` + `calypso-three-deck-c40` packing |

Out of scope for this dogfood: RevF ~90 m hull stretch, 160 m legacy, maritime PKG drawings. Full Boolean ring/sill/seal mesh remains blueprint-spec → future; openings use baseline splits + leaf geometry.

## Arrangement (engineering)

```
FP ── hab stack (−1/0/+1) ── WT-BH eng (38.8) ── Engineering OAH ── WT-BH hold (47) ── Hold 9 m ── AP
```

- Junctions emit **VEST-BR / VEST-P / VEST-S** with **three framed openings** each (never open T by deleting walls).
- Opening entities carry schedule `tag`, `W_open` / `H_open` / `H_sill`, `ClearWidth` / `ClearHeight`, `hostWallId`, `connects`.

## What is generated

- Full RevG room inventory with **partitions on every room**
- Individual **Crew Cabin 1–5** and **Berth 1–10** (via `arrayInstance` metadata + derived spaces/walls)
- Stairs / Elev shafts on all three decks
- **One** Engineering full-OAH void + **one** Cargo Void (9 m) with `continuousVoid` (visible across deck filters)
- Fore catwalk (full bay × 3 m) + twin CD hatches on armored hold BH + aft ramp **~12.99 × 4.0 × 3.2**
- **HILS-C40** stow: 15 `box` entities (`C40-c{col}-t{tier}`), 5 abreast × 1 deep × 3 high
- Vestibules + tagged opening schedule (PD-*/AH-*/CD-*/RAMP) with **wall baseline splits** (`OpeningDerivation`)
- Lore hooks: `Bridge`, `OwnerLock`, `PhotoWallBridge`, `ArmoryCrossing`, `GalleyGrowthChart`, `EngCore`, `CargoVoidEye`, `AftRamp`, airlocks, etc.
- Exterior nacelle pods for orbit silhouette
- Aft: large cargo **hatch-ramp** (no stern thruster bank)
- Side pods: main **engines** (aft nozzles) + **FTL graviton field manipulators** (mid-fore emitters)

## Detail pass (renderer)

Immediate-mode cubes/lines/cylinders (no DrawTriangle3D):

- Door / hatch **leaves** + aft **ramp steps**; armored tint on CD-*
- Module prop kits: cabin desk/webbing, galley appliance run, airlock dual hatches + hooks, triple corridor trunks, eng tanks/conduits, lounge bar/stools, cargo gantry/cleats/handrails
- Orbit: hull **panel seams + rivets**; nacelle **end caps**; C40 containers in orbit + cargo interior

## Run

```bash
dotnet run --project apps/cad/CalypsoCad
```

On startup regenerates under `%LocalAppData%\Novolis\CalypsoCad\generated\`:

- `calypso.cadlayers.json`
- `calypso.cadshapejson`
- `calypso.cadjson`

Headless Raylib PNG tour:

```bash
dotnet run --project apps/cad/CalypsoCad -- --headless
```

Camera walkthrough via **`Novolis.Raylib.Capture`** `FrameCaptureSession` (PNG frame stream after each `EndDrawing`; ffmpeg assembles MP4/GIF — no BeginVideo binding in this Raylib stack):

```bash
dotnet run --project apps/cad/CalypsoCad -- --walkthrough
```

Both stills and walkthrough (stills **2560×1440**, walkthrough **1920×1080**):

```bash
dotnet run --project apps/cad/CalypsoCad -- --headless --walkthrough
```

JSON only:

```bash
dotnet run --project apps/cad/CalypsoCad -- --generate-only --json-only
```

## Headless tour checklist

Exports land in `generated/exports/` as **stable overwrite names** (`{kind}.png`). Legacy `*-headless-*.png` files are purged on each headless run. Manual **Export PNG (E)** still writes a timestamped snap.

- [ ] `plan-deck-m1.png` / `plan-deck-0.png` / `plan-deck-p1.png`
- [ ] `orbit-bow-quarter.png` — high 3/4 bow
- [ ] `orbit-broadside.png` — low beam elevation
- [ ] `orbit-stern-quarter.png` — aft 3/4: hatch-ramp + side-pod engines
- [ ] `orbit-stern-on.png` — ramp face (no stern thruster bank)
- [ ] `orbit-ramp-close.png` / `orbit-pod-port.png` / `orbit-pod-stbd.png` / `orbit-pod-ftl.png`
- [ ] `orbit-broadside.png` / `orbit-low-pass.png` / `orbit-three-quarter-high.png`
- [ ] `orbit-cutaway-long.png` / `orbit-cutaway-beam.png`
- [ ] Interior solid: `interior-solid-{bridge,crossing,cabin1,galley,infirmary,stairs,engineering,cargoVoid,lounge,airlockPort,airlockStbd}.png`
- [ ] Interior section: `interior-cutaway-bridge.png` / `interior-cutaway-cargoVoid.png` / `interior-cutaway-engineering.png`
- [ ] DK0 catwalk POV: `catwalk-containers.png` / `catwalk-containers-quarter.png` / `catwalk-span.png` / `catwalk-passage-port.png` / `catwalk-passage-stbd.png`
- [ ] Walkthrough: `walkthrough/frame-*.png`, optional `walkthrough.mp4` / `walkthrough.gif`, keyframes `walkthrough-*.png`

**Cutaway (C):** world slicing plane; geometry on the camera side of the plane is not drawn. Orbit default is longitudinal (YZ cut). Interior cut is a vertical plane through the selected space center facing the eye.

**Catwalk presets:** standing eye on mid-deck (DK0) cargo catwalk; ensemble draws hold + C40 stack + twin corridors so you can look aft at containers or forward down a passageway.

## Explore (UI)

| Key / UI | Action |
|----------|--------|
| P / Plan | Top-down plan |
| O / Orbit | Exterior orbit (drag / wheel) |
| I / Interior | Camera inside selected space |
| W / C / S | Wire / cutaway / solid |
| 1 / 2 / 3 | Deck −1 / 0 / +1 filter |
| 0 | All decks |
| F | Fit orbit camera |
| E | Export current PNG |
| Spaces / Hooks lists | Select room or lore hook for interior camera |

Walls carry **two-sided** `shapeId`s (side A = left of baseline with +Y up).

## Packages

PackageReference only (`2026.1.*`): Avalonia, `Novolis.Avalonia.Raylib`, `Novolis.Raylib`, `Novolis.Raylib.Capture`, `Novolis.Rendering.Presentation.Silk`, `Novolis.Avalonia.Studio`.
