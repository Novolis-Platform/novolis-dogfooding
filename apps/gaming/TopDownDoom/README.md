# TopDownDoom

Dogfooding sample for **Doom combat grammar** in orthographic top-down: arena flow, keycard trap, monster closet, chunky weapons, explosive barrels, and readable encounter composition.

## Design rule

Every room should be a small Doom encounter you can read from above.

## Controls

| Input | Action |
|-------|--------|
| WASD | Move (high speed, no stamina) |
| Mouse | Aim |
| LMB | Shoot (hold) |
| Shift | Short dash (cooldown) |
| E | Cycle weapon |
| Esc | Pause |

## Packages

- `Novolis.Rendering.TwoD` + `Novolis.Rendering.Backends.TwoD.Silk` — orthographic XZ playfield with oblique wall extrusion
- `Novolis.Game.MenuFlows` — pause screen base (dogfood)

## Art

- **Default:** procedural marine / zombie / imp / bruiser sprites and pickup icons.
- **Optional:** purchase [Top Down Shooter: Main Characters](https://free-game-assets.itch.io/top-down-shooter-main-characters), unzip under `Assets/TdsCharacters/` (see `Assets/TdsCharacters/README.md`). The game detects `walk`+`gun` / `gun`+`shot` folders automatically.

## Run

```bash
dotnet run --project apps/gaming/TopDownDoom/TopDownDoom.csproj
```

Restore uses GitHub Packages (`2026.1.*`) only — see repo `NuGet.config`.
