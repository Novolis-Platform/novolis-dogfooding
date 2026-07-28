# TrampFreighterSim

Observational tramp freighter circuit — **job evaluation + variable speed**.

Self-contained. Consumes `Novolis.Economy.*` from GitHub Packages.

```powershell
dotnet run --project novolis-dogfooding/apps/economy/TrampFreighterSim
```

| Key | Speed |
|-----|-------|
| `Space` | Pause / resume |
| `1` … `6` | ½× → Warp |
| `Q` | Quit |

Autopilot quotes Ore F→C and Parts C→F (rev − cog − fuel − toll − crew), accepts only if Δ ≥ min margin, sells delivered cargo before the next haul, and rejects Sparse Rim as tank-infeasible instead of flying it for drama.
