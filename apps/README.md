# Dogfood apps

Small executables that reference library projects under `../submodules/` via `ProjectReference`.

Add a folder here, reference `..\..\submodules\novolis-<domain>\...`, and register the project in `Novolis.Dogfooding.slnx`.

## WireFish Viewer

Live packet capture UI for `Novolis.Transports.WireFish` (WireShark-style layout). Requires **Npcap** on Windows for capture devices.

```bash
dotnet run --project apps/WireFishViewer
```

## DoomLite3D

```bash
dotnet run --project apps/DoomLite3D
```
