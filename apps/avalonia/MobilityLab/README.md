# MobilityLab

Controlled tax–mobility lab on the Civics / Economy / Geopolitics kernels (Avalonia scientific UI — not a Spectre theatre clone).

## Abstract

Holding geography and initial stocks fixed, does raising polity Alpha’s household tax above the Economy tax-push / Civics emigration thresholds cause (1) net population outflow, (2) higher emigration pressure, and (3) weaker legitimacy versus a low-tax twin Beta, with Gamma as a low-tax destination?

Default treatment: α tax `0.38`, β tax `0.14`, γ tax `0.12`, 36 months, seed 42. An optional war-shock confounder is **off** by default so the primary claim stays identifiable.

## Identification strategy

- Twin / treatment–control: Alpha and Beta share matched province counts, initial populations (A0/A1 ↔ B0/B1), government form, and military baseline; only household tax differs.
- Gamma is a low-tax haven with higher immigration attractiveness.
- Treatment taxes are **re-locked every month** after optional fiscal agents so policy drift cannot confound the tax gap.
- Fiscal agents default **off**; war shock (Alpha–Beta declare war at month index 8) is a confounder switch.
- Spatial population truth remains on `Province.Population` via `PopulationMigration`; Civics demography and Economy cohort counts sync from owned population. See [demography-coupling.md](d:\novolis\novolis-civics\docs\demography-coupling.md).

## Month loop

1. Optional fiscal agents  
2. Lock treatment taxes  
3. Trade clearing  
4. Economy periods (α/β) → Civics delivery  
5. Gamma geo civic month  
6. `PopulationMigration.RunMonth`  
7. Optional conflict (if war shock active)  
8. Sample series (pop, net migration, emigration pressure, legitimacy, tax, prodVal, control)

## How to reproduce

```powershell
dotnet build d:\novolis\novolis-dogfooding\apps\avalonia\MobilityLab\MobilityLab.csproj -p:NovolisUseProjectReferences=true

dotnet run --project d:\novolis\novolis-dogfooding\apps\avalonia\MobilityLab\MobilityLab.csproj -p:NovolisUseProjectReferences=true

dotnet run --project d:\novolis\novolis-dogfooding\apps\avalonia\MobilityLab\MobilityLab.csproj -p:NovolisUseProjectReferences=true -- --headless 36
```

War-shock confounder (headless):

```powershell
dotnet run --project d:\novolis\novolis-dogfooding\apps\avalonia\MobilityLab\MobilityLab.csproj -p:NovolisUseProjectReferences=true -- --headless 36 --war
```

Local multi-repo builds use ProjectReference mode (`-p:NovolisUseProjectReferences=true`). Published consumers restore from nuget.org + GitHub Packages only.

## Interpretation of PASS/FAIL

| Check | Claim |
|-------|--------|
| α pop outflow | Net migration Σ &lt; −10k **or** end pop &lt; start pop |
| α pressure &gt; β | Peak and mean emigration pressure higher on Alpha |
| α L &lt; β L | End legitimacy weaker on treatment |
| γ haven / spatial move | Gamma gained population **or** telemetry migrated &gt; 0 |
| treatment locked | Spec tax gap still held at horizon |

Primary identification expects the first three checks with war shock **off**. FAIL on outflow/pressure with α tax above ~0.28 usually means kernel thresholds or seed geography need re-check; FAIL with war **on** may reflect conflict confounders rather than tax alone.
