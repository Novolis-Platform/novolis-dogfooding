# NearSolPolity

Near-Sol **tycoon slice** — four firms + treasury via `Novolis.Economy.Agents`, hub order book, thin finance loans.

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
| Carrier | `CarrierFirmAgent` |

SKU story (app only): Raw / Capital / Final / Energy. Travel: 1.3 d/ly. Agents are heuristic economic agents + `DeterministicRandom` — not ML.
