# Mesh Studio (MeshBench)

Avalonia mesh studio dogfooding **Novolis.Workspaces**, **Timeline**, **Snapshots**, **Rendering** path tracing, and **Audio** save feedback.

## Features

- **Viewport**: CPU path-traced orbit camera (drag orbit, wheel zoom, **Shift+drag** to move selected mesh on XZ)
- **Meshes**: Add box/sphere, duplicate, delete, named parts with stable selection
- **Inspector**: Edit name, position, size, and RGB color
- **Timeline**: Save points (zip + branch), restore, branch — **Ctrl+S**
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
