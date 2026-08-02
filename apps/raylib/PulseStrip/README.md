# PulseStrip

Anti-grav circuit racer homage (Wipeout-inspired): **spline circuits**, weapons/boost, procedural VFX + miniaudio SFX, and **evolutionary neural** opponents.

## Platforms

| Platform | Status |
|----------|--------|
| **Windows** (`win-x64`) | Supported — Raylib + miniaudio |
| **Linux** (`linux-x64`) | Supported — Raylib + miniaudio |
| **Android** | **Deferred** — Novolis.Raylib has no Android RID/host yet; shared logic lives in `PulseStrip.Core` for a future head |

## Run

```powershell
dotnet run --project d:\novolis\novolis-dogfooding\apps\raylib\PulseStrip -p:NovolisUseProjectReferences=true
```

Headless smoke (trains/loads brains, ticks a short race):

```powershell
$env:NOVOLIS_RAYLIB_HEADLESS = '1'
dotnet run --project d:\novolis\novolis-dogfooding\apps\raylib\PulseStrip -p:NovolisUseProjectReferences=true -- --smoke
```

## Controls

- **W/Up** throttle, **S/Down** brake, **A/D** steer
- **Shift** boost, **Space** fire plasma
- Mild auto-cruise when no throttle/brake is held

## Layout

- `PulseStrip.Core` — hover sim, pickups/weapons, ML trainer/`BrainStore`, `--smoke` runner
- `PulseStrip` — Raylib host, MenuFlows, FX, SFX
- `Content/brains/` — champion network JSON (created on first race/smoke)
- `Content/sfx/` — procedural WAVs generated at runtime

## CI / publish

- `.github/workflows/pulsestrip-smoke.yml` — test + headless smoke on Ubuntu
- `.github/workflows/pulsestrip-publish.yml` — `win-x64` / `linux-x64` publish artifacts
- Android job is intentionally disabled with a note until Raylib natives exist
