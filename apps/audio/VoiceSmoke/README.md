# VoiceSmoke

Dogfoods **Novolis.Audio.Voice** from GitHub Packages only (`2026.1.*`).

```bash
dotnet restore
dotnet run --project apps/audio/VoiceSmoke
dotnet run --project apps/audio/VoiceSmoke -- --null
dotnet run --project apps/audio/VoiceSmoke -- --calm
```

Default: **`excitable_female`** archetype + ATC radio delivery (`Novolis.Audio.Voice.Profiles` + `Novolis.Dogfooding.Voice`). `--calm` uses **`neutral_female`** dry (no radio/phraseology).

Requires `Novolis.Audio.Voice.SherpaOnnx` **2026.1.3+** (three bundled Piper zips extract on build).
