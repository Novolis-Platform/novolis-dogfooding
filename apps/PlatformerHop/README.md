# PlatformerHop

Raylib pseudo-3D side-view platformer dogfooding **Novolis.Raylib**, **Novolis.Math.Arrays**, **Novolis.Simulation.Kinematics**, and **Novolis.Simulation.View**.

Prefer orthographic 2D: see **PlatformerTwoD** (Silk backend, shared `SideLevel`).

## Run

```powershell
cd novolis-dogfooding
dotnet run --project apps/PlatformerHop
```

## Controls

| Input | Action |
|-------|--------|
| A / D | Move |
| Space / W | Jump |
| R | Reset level |
| F3 | Diagnostics overlay |

## ProjectRef note

For local iteration against sibling repos, open `Novolis.Platform.slnx` or pass `-p:NovolisUseProjectReferences=true`. Committed `.csproj` files use `PackageReference` from GitHub Packages only.
