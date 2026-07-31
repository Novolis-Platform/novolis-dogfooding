# RagdollPlay

Interactive ragdoll room dogfooding **Novolis.Raylib**, **Novolis.Math.Arrays**, **Novolis.Simulation.*** (World, Builders, View), **Novolis.Physics.Collision.Simple**, and **Novolis.Physics.Joints**.

## Run

```powershell
cd novolis-dogfooding
dotnet run --project apps/RagdollPlay
```

## Controls

| Input | Action |
|-------|--------|
| LMB | Shove ragdoll |
| MMB drag | Orbit camera |
| Mouse wheel | Zoom |
| R | Reset pose |
| F3 | Diagnostics overlay |

## ProjectRef note

For local iteration against sibling repos, open `Novolis.Platform.slnx` or pass `-p:NovolisUseProjectReferences=true`. Committed `.csproj` files use `PackageReference` from GitHub Packages only.
