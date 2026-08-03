# MusicMakerLab

Magix Music Maker / Audacity–style arrangement plus a MIDI piano with a large instrument bank.

## Tabs

### Arrangement
- Sound library with mini waveforms
- Multi-track arrangement + playhead
- Clip envelope — gain, fade in/out
- Split at playhead / remove clip
- Play mix preview (NAudio) + export mix WAV

### MIDI Piano
- 50+ parametric sounds (keys, leads, bass, pads, pluck, bell, brass, wind, perc, FX)
- On-screen keyboard + computer keys (A–K, Z/X octave)
- Record a take → play back
- Save/load **MIDI** (`.mid`), **patch** / **bank** JSON
- **Bounce WAV to library** for use on the Arrangement tab

## Run

```powershell
dotnet run --project d:\novolis\novolis-dogfooding\apps\avalonia\MusicMakerLab\MusicMakerLab.csproj -p:NovolisUseProjectReferences=true
```

## Walkthrough

1. Play the seeded Lead + Pad arrangement.
2. Open **MIDI Piano**, pick **Bright Piano**, click keys or use A–K.
3. **Record / Stop** a short take → **Play take** → **Save MIDI…** or **Bounce WAV to library**.
4. Back on **Arrangement**, place the bounced sound on a track → **Export mix WAV…**.
