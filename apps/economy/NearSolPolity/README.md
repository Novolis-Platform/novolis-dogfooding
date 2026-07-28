# NearSolPolity

Near-Sol **tycoon slice** — extractive / manufacturing / retail / **8-tramp fleet** / **households** via `Novolis.Economy.Agents`, habitats (`EconomicRegion`), hub order book, thin finance loans.

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
| Carrier + Tramp2…8 | `CarrierFirmAgent` (homes cycle Sol / mines / plants) |
| Cohorts | `HouseholdFirmAgent` (comfort invest into Mining float) |

**Consumption sink:** households spend only on **Final** (Goods). Station shelves at Capital, Inhabited, and Mining camps. Capital parts stay B2B (plant → mine). Wages refill `BudgetRemaining`; retail destroys Final stock — that loop is the intended equilibrium / growth driver.

SKU story (app only): Raw / Capital / Final / Energy. Travel: 1.3 d/ly. Agents are heuristic + `DeterministicRandom` — not ML.
