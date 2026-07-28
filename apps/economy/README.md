# Economy dogfood

| App | Package surface |
|-----|-----------------|
| [EconomyBoard](EconomyBoard/) | Commodity-chain Avalonia board (`Economy.*` kernel) |
| [TrampFreighterPlay](TrampFreighterPlay/) | Spectre tramp freighter on hub/corridor transport |

```powershell
dotnet run --project apps/economy/EconomyBoard
dotnet run --project apps/economy/TrampFreighterPlay
```

Self-contained scenarios (duplication intentional). No Astro coupling.
