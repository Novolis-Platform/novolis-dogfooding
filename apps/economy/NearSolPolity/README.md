# NearSolPolity

Near-Sol **tycoon slice** — extractive / manufacturing / retail / **tramp fleet** / **households** via `Novolis.Economy.Agents`, habitats (`EconomicRegion`), hub order book, thin finance loans.

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
| Carrier + Tramp2/3 | `CarrierFirmAgent` (one hull each; homes at Sol / mine / plant) |
| Cohorts | `HouseholdFirmAgent` (comfort invest/lend on `BudgetRemaining`) |

SKU story (app only): Raw / Capital / Final / Energy. Travel: 1.3 d/ly. Agents are heuristic economic agents + `DeterministicRandom` — not ML. Manufacturing labor comes from region household pools (Mean productivity); carriers keep crew `SetLabor`.

A single tramp with a high min-margin floor tends to stall after early liquidity clears (long-haul Δ below floor → plants starve → ×0 production). The fleet + lower `MinMargin` keeps book spreads moving.
