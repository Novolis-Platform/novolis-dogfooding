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

## What it does

1. Loads embedded `data/nearsol-100.json` into a `StarCatalog`.
2. Builds a ≤12 ly hop graph (`RangeBandCostModel`: short ≤10 @1×, long ≤12 @3×).
3. Assigns roles: Capital / Inhabited / Industrial / Mining / Transit / Waypoint.
4. Maps systems → `TransportHub`s and hops → directed corridors (**7 days/ly** → hours in the kernel).
5. Seeds polity production (ore → parts → goods), cohorts, fuel bunkers.
6. Runs a polity controller (plans, prices, ore feeders, mine stock caps) and a graph-aware tramp autopilot.

Example transit: Sol → α Centauri (~4.4 ly) ≈ **31 days**. Fuel burn is scaled so a tank of 6 still covers short ≤10 ly hops; longer legs bunker at transit hubs.

Ore hauls settle B2B via Economy `TransferGoodsForCash` at a freight unit, not consumer retail. Mines idle above a stock cap.

**Circularity:** mines consume parts (maintenance) to produce ore; industry turns ore into parts, goods, and refined fuel; polity feeders + tramp haul ore plant-ward and parts mine-ward. Fuel is refined in-polity when possible; exogenous bunker buys are a last-resort import.

**Credits (closed loop):** Economy policy `HouseholdCreditFromWages` + `CarryForward` + `TollBeneficiaryFirmId`. Opening firm cash + household float is the money stock. Paid wages become household spending power; consumer purchases return cash to firms; tolls accrue to the co-op.
