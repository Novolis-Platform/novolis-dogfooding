# HumanoidLab

Avalonia dogfood for `Novolis.Simulation.Humanoid` + `Novolis.Simulation.Humanoid.Physics`.

Three panes:

1. **Walk** — procedural clip → FK → capsule mannequin (side view)
2. **Ragdoll** — settled sphere ragdoll + **AdaptiveMesh** person hull (`HumanoidAdaptiveBody`)
3. **Bow** — bow-draw clip + `TwoBoneIk` + mannequin + bow overlay

```powershell
dotnet run --project novolis-dogfooding/apps/avalonia/HumanoidLab -p:NovolisUseProjectReferences=true
```

Use ProjectReference mode until Humanoid packages are on GitHub Packages.

## HTTP control (agent tooling)

Starts on `http://127.0.0.1:18765/` (override with `HUMANIOD_LAB_HTTP_PORT`). Marker: `%TEMP%/novolis-humanoid-lab-http.txt`.

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/health` | Liveness |
| GET | `/ragdoll` | Speeds, KE, bone error, sleep, hip |
| POST | `/ragdoll/reset` | Respawn standing |
| POST | `/ragdoll/tip` | Body `{ "impulse": [x,y,z] }` optional |
| POST | `/ragdoll/entropy` | Body `{ "rate": 3.2, "autoTip": true }` |
