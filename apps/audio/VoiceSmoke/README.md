# VoiceSmoke

Console dogfood for **Novolis.Audio.Voice** from GitHub Packages only (`2026.1.*`).

Synthesizes sample phrases with bundled Piper models via Sherpa ONNX.

## Run

```bash
dotnet restore
dotnet run --project apps/audio/VoiceSmoke
dotnet run --project apps/audio/VoiceSmoke -- --null
dotnet run --project apps/audio/VoiceSmoke -- --calm
```

| Flag | Behavior |
|------|----------|
| *(default)* | **`excitable_female`** archetype + ATC radio delivery |
| `--null` | Null voice backend (no synthesis) |
| `--calm` | **`neutral_female`** dry delivery (no radio/phraseology) |

Uses `Novolis.Audio.Voice.Profiles` and `Novolis.Dogfooding.Voice`. Requires `Novolis.Audio.Voice.SherpaOnnx` **2026.1.3+** (three bundled Piper zips extract on build).

## Related

| App | Role |
|-----|------|
| [NovolisVoiceStudio](NovolisVoiceStudio/) | Archetype editor and C# export |
| [ManuscriptSmoke](../manuscript/ManuscriptSmoke/) | Speech planner dry-run |
