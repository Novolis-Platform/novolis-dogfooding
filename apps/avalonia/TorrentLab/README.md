# Torrent Lab

Dogfood for `Novolis.Transports.Torrent` and `TorrentSessionPanel` / `TorrentProgressView`.

## Sample payload

Tiny Core Linux **Core-current.iso** (~18 MB) lives under `samples/`. Create the `.torrent` and prove local seed→leech:

```powershell
dotnet run --project novolis-dogfooding/apps/avalonia/TorrentLab/TorrentLab.csproj -c Release -p:NovolisUseProjectReferences=true -- --smoke
```

## UI

```powershell
dotnet run --project novolis-dogfooding/apps/avalonia/TorrentLab/TorrentLab.csproj -c Release -p:NovolisUseProjectReferences=true
```

Then **Load Core sample…** → **Start**.
