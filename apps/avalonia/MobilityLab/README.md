# MobilityLab

Controlled tax–mobility lab on the Civics / Economy / Geopolitics kernels (Avalonia scientific UI — not a Spectre theatre clone).

## Abstract

Holding geography and initial stocks fixed, does raising polity Alpha’s household tax above the Economy tax-push / Civics emigration thresholds cause (1) net population outflow, (2) higher emigration pressure, and (3) weaker early approval vs low-tax twin Beta, with Gamma as a low-tax destination?

Default treatment: α tax `0.38`, β tax `0.14`, γ tax `0.12`, 36 months, seed 42. An optional war-shock confounder is **off** by default so the primary claim stays identifiable.

## Identification strategy

- **Twin / treatment–control:** Alpha and Beta share matched province counts, near-equal initial populations, government form, and military baseline; only household tax differs.
- **Same-seed counterfactual:** a second world where Alpha tax = Beta tax (no treatment gap). Primary causal estimand is **ATT** on Alpha outcomes (treated − CF).
- **Within-world DID:** Alpha vs Beta population growth rates in the treated world.
- Gamma is a low-tax haven with higher immigration attractiveness.
- Treatment taxes are **re-locked every month** after optional fiscal agents.
- Fiscal agents default **off**; war shock is a confounder switch (identification check fails when on).
- Spatial population truth: `Province.Population` via `PopulationMigration`. See [demography-coupling.md](d:\novolis\novolis-civics\docs\demography-coupling.md).

## Estimands (reported)

| Estimand | Definition |
|----------|------------|
| ATT Alpha pop | Treated Alpha end pop − CF Alpha end pop (also % of baseline) |
| ATT mean push | Post-burn-in (after M6) mean emigration pressure: treated − CF |
| DID pop growth | Alpha relative Δpop − Beta relative Δpop (treated world) |
| Gamma absorb share | Gamma Δpop / (−Alpha Δpop) when Alpha loses people |
| Early approval gap | Mean approval Alpha−Beta over M1–M18 (tax channel; not end L) |

End-horizon legitimacy is a **diagnostic only** — both polities often sit near the 1.0 ceiling.

## Month loop

1. Optional fiscal agents  
2. Lock treatment taxes  
3. Trade clearing  
4. Economy periods (α/β) → Civics delivery  
5. Gamma geo civic month  
6. `PopulationMigration.RunMonth`  
7. Optional conflict (if war shock active)  
8. Sample series → evaluate vs counterfactual

## How to reproduce

```powershell
dotnet build d:\novolis\novolis-dogfooding\apps\avalonia\MobilityLab\MobilityLab.csproj -p:NovolisUseProjectReferences=true

dotnet run --project d:\novolis\novolis-dogfooding\apps\avalonia\MobilityLab\MobilityLab.csproj -p:NovolisUseProjectReferences=true

dotnet run --project d:\novolis\novolis-dogfooding\apps\avalonia\MobilityLab\MobilityLab.csproj -p:NovolisUseProjectReferences=true -- --headless 36
```

Headless prints a markdown report with identification, effect sizes, and PASS/FAIL. Lab UI: **Copy markdown report**.

War-shock confounder (expect identification FAIL):

```powershell
dotnet run --project d:\novolis\novolis-dogfooding\apps\avalonia\MobilityLab\MobilityLab.csproj -p:NovolisUseProjectReferences=true -- --headless 36 --war
```

## Interpretation of PASS/FAIL

| Check | Claim |
|-------|--------|
| identification | CF valid, war off, tax locked, twin balance at M1 |
| ATT Alpha pop &lt; 0 | Primary causal: high tax cuts Alpha pop vs CF |
| DID Alpha vs Beta growth | Twin contrast: Alpha grows slower / shrinks more than Beta |
| ATT + twin pressure | Higher push vs CF and vs Beta post burn-in |
| Gamma absorbs outflow | Haven destination mechanism |
| early approval | Tax channel on civic approval (not ceiling L) |

Primary scientific claim is **ATT Alpha pop** with war off. Long horizons amplify outflow magnitude; use ATT/% of baseline, not only end-stock PASS.
