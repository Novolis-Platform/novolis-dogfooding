# Torrent Lab

Avalonia dogfood for `Novolis.Transports.Torrent` and `TorrentSessionPanel` / `TorrentProgressView`.

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

## What it exercises

| Component | Role |
|-----------|------|
| `Novolis.Transports.Torrent` | BitTorrent session engine |
| `TorrentSessionPanel` | Session controls in Avalonia |
| `TorrentProgressView` | Transfer progress UI |

## Related

| Package | Role |
|---------|------|
| `Novolis.Avalonia.Controls` | Torrent UI controls |
