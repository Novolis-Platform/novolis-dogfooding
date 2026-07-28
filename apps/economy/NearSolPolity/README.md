# NearSolPolity

Near-Sol interstellar polity — **100 closest stars** (Johnston 2022 galactic XYZ) bridged from `Novolis.Astro.*` into `Novolis.Economy.*` logistics, production, and tramp trade.

Economy packages stay Astro-free; this app owns the mapping.

```powershell
dotnet run --project novolis-dogfooding/apps/economy/NearSolPolity
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
4. Maps systems → `TransportHub`s and hops → directed corridors (1 ly/day → hours).
5. Seeds polity production (ore → parts → goods), cohorts, fuel bunkers.
6. Runs a polity controller (plans, prices, ore feeders, mine stock caps) and a graph-aware tramp autopilot.

Ore hauls settle B2B at a **freight unit** (`OreBuy` + thin premium), not consumer retail. Mines idle above a stock cap. Tramp stays thin-margin.

**Circularity:** mines consume parts (maintenance) to produce ore; industry turns ore into parts, goods, and refined fuel; polity feeders + tramp haul ore plant-ward and parts mine-ward. Fuel is refined in-polity when possible; exogenous bunker buys are a last-resort import.

**Credits (closed loop):** opening firm cash + household float is the money stock. Period budget resets are disabled (no daily mint). Paid wages become household spending power; consumer purchases return cash to firms; tolls accrue to the co-op.

Non-TTY advances **500h** by default (pass a hour count as argv). Prints liquid stock vs opening.
