# MusicMakerLab

Lightweight Magix Music Maker / Audacity–style dogfood for `Novolis.Audio.Edit` + `Novolis.Avalonia.Audio`.

## Features

- **Sound library** with mini waveforms
- **Multi-track arrangement** + playhead
- **Clip envelope** — gain, fade in/out
- **Split** at playhead / remove clip
- **Play** mix preview (NAudio)
- **Export mix WAV…**

## Run

```powershell
dotnet run --project d:\novolis\novolis-dogfooding\apps\avalonia\MusicMakerLab\MusicMakerLab.csproj -p:NovolisUseProjectReferences=true
```

## Walkthrough

1. Play the seeded Lead + Pad arrangement.
2. Select **G4 spare** in the library → **Add to track** (or double-click).
3. Click a clip → tweak fade/gain → **Apply envelope**.
4. **Split at playhead**, then **Export mix WAV…**.
