# Novolis Voice Studio

Export-only v1: tweak voice archetypes and ATC delivery, preview with Sherpa Piper, copy C# for [`VoiceArchetypeCatalog`](https://github.com/Novolis-Platform/novolis-audio) and app delivery types.

## Run

```bash
dotnet run --project apps/audio/NovolisVoiceStudio/NovolisVoiceStudio.csproj
```

Requires bundled Piper models (extracted on first run). Uses `Novolis.Audio.Voice.Design` and `Novolis.Avalonia.Voice` from GitHub Packages (`2026.1.*`).

## Workflow

1. Select a catalog preset or **Clone** / **New**.
2. Adjust profile id, Piper model, speaking rate, and ATC DSP sliders.
3. **Play** to hear the preview phrase.
4. Pick an export template (Archetype / ATC / Usage / Bridge) and **Copy** into your library source.
