# Mesh Studio (MeshBench)

Avalonia mesh studio dogfooding **Novolis.Workspaces**, **Timeline**, **Snapshots**, **Rendering** (Raylib preview + ILGPU path trace), and **Audio** save feedback.

## Features

- **Dual viewport**: **Preview** (Raylib, instant while editing) and **Quality** (ILGPU path trace — opt-in via toolbar)
- **History**: git-graph with branch colors, snapshot-kind badges, and a **●** dot on HEAD (you are here)
- **Meshes**: Add box/sphere, duplicate, delete, named parts with stable selection
- **Inspector**: Debounced edits for position, size, and RGB (no full scene compile per slider tick)
- **Timeline**: Save points (fast `scene.json`, zip snapshot in background), restore, branch — **Ctrl+S**
- **Fit view** (**F**) frames all meshes

## Shortcuts

| Key | Action |
|-----|--------|
| Ctrl+S | Save point |
| Ctrl+D | Duplicate selection |
| Delete | Delete selection |
| F | Fit view |
| B | Add box |
| S | Add sphere |

## Run

```bash
dotnet run --project apps/rendering/MeshBench/MeshBench.csproj
```

Default workspace: `%LocalAppData%\Novolis\MeshBench\default-workspace`

## Performance smoke test

1. Open Mesh Studio — **Preview** mode should show meshes immediately.
2. Drag an inspector color slider — preview stays smooth; quality rebuild debounces (~100 ms).
3. Click **Quality** only when you want path tracing (editing always returns to **Preview**).
4. **Shift+drag** a mesh — preview updates every frame.
5. **Ctrl+S** — history panel shows a git-graph commit line.
6. Left panel shows `git log --graph` style history (or a hint until first save).

Optional: `NOVOLIS_RAY_BACKEND=cpu` forces CPU path tracing for comparison.
