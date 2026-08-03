# MovieMakerLab

Minimal Windows Movie Maker–style dogfood: collections, storyboard, preview monitor, and transport.

Exercises `Novolis.Video.Edit` + `Novolis.Avalonia.Video`.

## Run

```powershell
dotnet run --project d:\novolis\novolis-dogfooding\apps\avalonia\MovieMakerLab\MovieMakerLab.csproj -p:NovolisUseProjectReferences=true
```

Use ProjectReference mode until `Novolis.Video.Edit` is published to GitHub Packages.

## Basics

- **Import pictures** — stills into collections + storyboard
- **Make color card** — solid title cards (always previewable)
- **Split at playhead** / **Remove clip**
- **Play / Pause** + storyboard scrub
