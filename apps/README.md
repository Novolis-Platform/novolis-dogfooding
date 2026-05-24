# Dogfood apps

Small executables that consume **published Novolis packages** from GitHub Packages (`PackageReference` in each `.csproj`).

Add a folder under `apps/`, declare packages in `Directory.Packages.props`, and register the project in `Novolis.Dogfooding.slnx`.

## WireFish Viewer

Live packet capture UI for `Novolis.Transports.WireFish` (WireShark-style layout). Requires **Npcap** on Windows for capture devices.

```bash
dotnet run --project apps/WireFishViewer
```

## DoomLite3D

```bash
dotnet run --project apps/DoomLite3D
```
