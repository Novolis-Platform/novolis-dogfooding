# NearSolPolity

Near-Sol interstellar polity — **100 closest stars** (Johnston 2022 galactic XYZ) bridged from `Novolis.Astro.*` into `Novolis.Economy.*` logistics, production, and tramp trade.

Economy packages stay Astro-free; this app owns the mapping.

```powershell
# Interactive Spectre Live dashboard
dotnet run --project novolis-dogfooding/apps/economy/NearSolPolity

# Headless: advance then print a plain report (no Live UI)
dotnet run --project novolis-dogfooding/apps/economy/NearSolPolity -- --headless 100d
dotnet run --project novolis-dogfooding/apps/economy/NearSolPolity -- --headless 2000h
```

| Key | Speed |
|-----|-------|
| `Space` | Pause / resume |
| `1` … `6` | ½× → Warp |
| `Q` | Quit |

## Abstractions SKUs

| Id | Story |
|----|--------|
| Ore | **Raw** materials |
| Parts | **Capital** / intermediate (mine maintenance + light retail) |
| Goods | **Final** goods (& services abstracted) |
| Fuel | **Energy** |

## What it does

1. Loads embedded `data/nearsol-100.json` into a `StarCatalog`.
2. Builds a ≤12 ly hop graph (`RangeBandCostModel`: short ≤10 @1×, long ≤12 @3×).
3. Assigns roles: Capital / Inhabited / Industrial / Mining / Transit / Waypoint.
4. Maps systems → hubs/corridors at **1.3 days/ly**; each system is its own demand area.
5. Seeds co-op production (raw → capital → final + energy), cohorts, fuel bunkers.
6. Runs polity controller (pressure-priced retail, circular feeders) and tramp autopilot.

**B2B freight** is distance-priced: `gate + haul(variable)/qty + premium` via `HaulCostEstimator` + `TransferGoodsForCash`.

**Credits:** `HouseholdCreditFromWages` + `CarryForward` + toll treasury; wages retuned so household budgets can recirculate.
