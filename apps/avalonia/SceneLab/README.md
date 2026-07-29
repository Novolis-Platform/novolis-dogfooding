# SceneLab

Dogfood host for **Novolis.Avalonia.3D** — C4D-inspired mesh modeller (Object Manager, primitives, wireframe poly edit, Array/Boole, Look).

## Run

```powershell
dotnet run --project apps/avalonia/SceneLab -p:NovolisUseProjectReferences=true
dotnet run --project apps/avalonia/SceneLab -p:NovolisUseProjectReferences=true -- --corvette
dotnet run --project apps/avalonia/SceneLab -p:NovolisUseProjectReferences=true -- --edit
dotnet run --project apps/avalonia/SceneLab -p:NovolisUseProjectReferences=true -- --gallery
dotnet run --project apps/avalonia/SceneLab -p:NovolisUseProjectReferences=true -- --cloner
dotnet run --project apps/avalonia/SceneLab -p:NovolisUseProjectReferences=true -- --boole
dotnet run --project apps/avalonia/SceneLab -p:NovolisUseProjectReferences=true -- --look
```

Same-repo sample (ProjectReference): `novolis-avalonia/samples/SceneLab`.

When using `-p:NovolisUseProjectReferences=true`, SceneLab also PackageReferences Raylib runtime packages explicitly (ProjectRef mode is non-transitive for NuGet graphs — same pattern as CalypsoCad).

## Troop Corvette (shipyard sample)

One hard-surface mesh built keel → bow → mid → truss array → stern → Boole cuts → engines → greeble arrays.

```powershell
dotnet run --project apps/avalonia/SceneLab/tools/TroopCorvetteBuilder -p:NovolisUseProjectReferences=true -- `
  apps/avalonia/SceneLab/samples/corvette-stages `
  apps/avalonia/SceneLab/samples/troop-corvette.nov3djson
```

Stages: `samples/corvette-stages/corvette-stage-01..08.nov3djson`. Final: `samples/troop-corvette.nov3djson`.

## Modeling

- **Edit modes:** Object / Point / Edge / Polygon (+ Make Editable)
- **Display:** Wireframe / Points / Isoline
- **Viewport:** click to pick (Shift multi-select), Alt+LMB or MMB orbit, drag gizmo to translate
- **Primitives:** Box, Sphere, Cylinder, Cone, Plane, Capsule, Torus, Pyramid, Disc, Tube, Platonics, Landscape
- **Mesh tools:** Extrude, Inset, Bevel, Bridge, Dissolve, Knife, Weld, Optimize, Subdiv

## LLM session

HTTP **18785** + TCP JSONL **18786**.

```bash
curl http://127.0.0.1:18785/session/hello
curl -X POST http://127.0.0.1:18785/session/command -H "content-type: application/json" \
  -d "{\"actionId\":\"open\",\"path\":\"D:/novolis/novolis-dogfooding/apps/avalonia/SceneLab/samples/troop-corvette.nov3djson\"}"
```

MCP (AvaloniaAgentMcp): `scene_hello`, `scene_snapshot`, `scene_actions`, `scene_command`, `scene_definition`, `scene_hosts`, `scene_http_connect`.
