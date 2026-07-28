# NearSolPolity

Near-Sol **tycoon slice** — extractive / manufacturing / retail / **7-tramp fleet** / **households** via `Novolis.Economy.Agents`, habitats (`EconomicRegion`), hub order book, thin finance loans.

```powershell
dotnet run --project novolis-dogfooding/apps/economy/NearSolPolity
dotnet run --project novolis-dogfooding/apps/economy/NearSolPolity -- --headless 100d
dotnet run --project novolis-dogfooding/apps/economy/NearSolPolity -- --headless 1000d
```

| Firm | Library agent |
|------|----------------|
| Mining | `ExtractiveFirmAgent` |
| Industry | `ManufacturingFirmAgent` |
| Station | `RetailFirmAgent` + `TreasuryFirmAgent` |
| Carrier + Tramp2…7 | `CarrierFirmAgent` (one hull each; homes cycle Sol / mines / plants) |
| Cohorts | `HouseholdFirmAgent` (comfort invest into Mining float) |

SKU story (app only): Raw / Capital / Final / Energy. Travel: 1.3 d/ly. Agents are heuristic economic agents + `DeterministicRandom` — not ML.

**Pressures:** region labor pools need mining-camp households (area-local); wider delivered spreads + `MinMargin` 0.4; lean firm cash + Civic treasury loans; household comfort invest into Mining ownership float. Manufacturing labor from Mean pools; carriers keep crew `SetLabor`.
