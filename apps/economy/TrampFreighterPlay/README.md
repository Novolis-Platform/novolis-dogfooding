# TrampFreighterPlay

Interactive dogfood for economic transport — independent tramp on a starport hub network (space skin, **no** Astro packages).

Consumes published `Novolis.Economy.*` from GitHub Packages (`2026.1.*`). Requires the transport kernel (hubs/corridors/`PlanShipment`) on the feed.

```powershell
dotnet run --project apps/economy/TrampFreighterPlay
```

| Key | Action |
|-----|--------|
| `1` | Advance 1 hour |
| `D` | Advance 24 hours |
| `J` | Speculative ore job Frontier → Core |
| `R` | Return parts Core → Frontier |
| `X` | Attempt Sparse Rim (expect plan failure) |
| `B` | Procure bunker fuel at Waystation |
| `S` | Post ore retail ask at Core + run sales |
| `Q` | Quit |
