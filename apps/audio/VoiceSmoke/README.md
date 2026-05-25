# VoiceSmoke

Dogfoods published **Novolis.Audio.Voice** packages from GitHub Packages (`2026.1.*`).

```bash
dotnet run --project apps/audio/VoiceSmoke
dotnet run --project apps/audio/VoiceSmoke -- --null    # silent synth/playback (automation)
dotnet run --project apps/audio/VoiceSmoke -- --wav     # also write temp WAV
```

Requires a direct `PackageReference` to `Novolis.Audio.Voice.SherpaOnnx` so bundled Piper model content is copied to the output directory.
