# NearSolPolity

Near-Sol **tycoon slice** — four firms (Mining / Industry / Station / Carrier), hub order book, heuristics + RNG only.

```powershell
dotnet run --project novolis-dogfooding/apps/economy/NearSolPolity
dotnet run --project novolis-dogfooding/apps/economy/NearSolPolity -- --headless 100d
dotnet run --project novolis-dogfooding/apps/economy/NearSolPolity -- --headless 1000d
```

| Firm | Role |
|------|------|
| Mining | Extract Raw; buy Capital; sell Raw on hub book |
| Industry | Buy Raw; make Capital/Final/Energy; sell on book |
| Station | Retail + toll treasury + bunkers; buy Final |
| Carrier | Clear sell@A + buy@B spreads (haul) |

SKU story: Raw / Capital / Final / Energy. Travel: 1.3 d/ly. Agents never use ML — only thresholds + `DeterministicRandom` jitter.
