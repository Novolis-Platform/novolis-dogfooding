# Economy dogfood

| App | Package surface |
|-----|-----------------|
| [EconomyBoard](EconomyBoard/) | Commodity-chain Avalonia board (`Economy.*` kernel) |
| [TrampFreighterPlay](TrampFreighterPlay/) | Spectre tramp freighter (interactive keys) |
| [TrampFreighterSim](TrampFreighterSim/) | Spectre tramp observer (variable speed + autopilot) |

```powershell
dotnet run --project apps/economy/EconomyBoard
dotnet run --project apps/economy/TrampFreighterPlay
dotnet run --project apps/economy/TrampFreighterSim
```

Self-contained scenarios (duplication intentional). No Astro coupling.
