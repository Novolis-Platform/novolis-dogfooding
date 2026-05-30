# Mesh Studio (MeshBench)

Avalonia mesh studio dogfooding **Novolis.Workspaces**, **Timeline**, **Snapshots**, **Rendering** (Raylib preview + ILGPU path trace), and **Audio** save feedback.

## Features

- **Dual viewport**: **Preview** (Raylib, ~60 FPS while editing) and **Quality** (ILGPU path trace after ~400 ms idle or when pinned)
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
3. Stop editing for ~0.5 s — switches to **Quality** and path trace refines (status shows ILGPU / sample count).
4. **Shift+drag** a mesh — preview updates every frame; one quality rebuild on release.
5. **Ctrl+S** — UI flashes quickly; timeline updates when zip finishes.
6. Toolbar **Preview** / **Quality** pins the active mode.

Optional: `NOVOLIS_RAY_BACKEND=cpu` forces CPU path tracing for comparison.
