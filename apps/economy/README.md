# Economy dogfood

| App | Package surface |
|-----|-----------------|
| [EconomyBoard](EconomyBoard/) | Commodity-chain Avalonia board (`Economy.*` kernel) |
| [TrampFreighterPlay](TrampFreighterPlay/) | Spectre tramp freighter (interactive keys) |
| [TrampFreighterSim](TrampFreighterSim/) | Spectre tramp observer (variable speed + autopilot) |
| [NearSolPolity](NearSolPolity/) | Near-Sol polity — Astro catalog → Economy hubs/production/tramp |

```powershell
dotnet run --project apps/economy/EconomyBoard
dotnet run --project apps/economy/TrampFreighterPlay
dotnet run --project apps/economy/TrampFreighterSim
dotnet run --project apps/economy/NearSolPolity
```

Self-contained scenarios (duplication intentional). NearSolPolity is the Astro↔Economy bridge at the dogfood layer.
