# MusicMakerLab

Magix Music Maker / Audacity–style arrangement plus a **piano score** (piano-roll + grand staff) with QuestPDF export.

## Tabs

### Arrangement
- Sound library with mini waveforms
- Multi-track arrangement + playhead
- Clip envelope — gain, fade in/out
- Split at playhead / remove clip
- Play mix preview (NAudio) + export mix WAV

### Piano Score
- Grand-staff full score preview
- Editable **piano-roll** (click to place notes, Alt/right-click or Delete to remove)
- 50+ parametric sounds
- On-screen keyboard + computer keys (A–K, Z/X octave)
- Record onto the score · Play score
- Save/load **MIDI**, patch/bank JSON
- **Export PDF…** via QuestPDF (staff systems + piano-roll + note list)
- Bounce WAV to Arrangement library

## Run

```powershell
dotnet run --project d:\novolis\novolis-dogfooding\apps\avalonia\MusicMakerLab\MusicMakerLab.csproj -p:NovolisUseProjectReferences=true
```
