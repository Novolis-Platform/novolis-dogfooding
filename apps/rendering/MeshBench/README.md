# MeshBench

Avalonia dogfood for **Novolis.Workspaces**, **Novolis.Timeline**, and **Novolis.Snapshots**, combined with **Novolis.Rendering** path tracing and a short **Novolis.Audio** save chime.

## Features

- Orbit camera over a CPU path-traced scene (boxes and spheres on a ground plane)
- Add/delete mesh primitives; scene stored in `documents/scene.json`
- **Ctrl+S** or toolbar: workspace save point (zip snapshot + timeline node)
- Restore and branch from the timeline list
- Audio feedback on save

## Run

```bash
dotnet run --project samples/MeshBench/MeshBench.csproj
```
