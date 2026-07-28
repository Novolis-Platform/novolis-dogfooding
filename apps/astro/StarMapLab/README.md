# StarMapLab

Dogfoods **Novolis.Avalonia.StarMap** with **Novolis.Astro** (catalog, routing, assessment, overlay) and **Physics.Astro** from GitHub Packages (`2026.1.*`).

Avalonia lab: **84 real nearby stars** (≤ ~20.5 ly) with Johnston (2022) galactic XYZ, pan/zoom map (X–Y galactic plane), From/To routing with prototype bands (**short ≤10 ly @ 1×**, **long ≤12 ly @ 3×** — long single hops cost more than several short ones), selectable jumps with `key=value` parsable detail, assessment/overlay side panel.

Default route: Sol → Altair. Check **List all graph jumps** to select any edge in the hop graph.

```powershell
dotnet run --project novolis-dogfooding/apps/astro/StarMapLab
```
