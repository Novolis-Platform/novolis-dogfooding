# VoiceSmoke

Dogfoods **Novolis.Audio.Voice** from GitHub Packages only (`2026.1.*`).

```bash
dotnet restore
dotnet run --project apps/audio/VoiceSmoke
dotnet run --project apps/audio/VoiceSmoke -- --null
```

Requires a direct `PackageReference` to `Novolis.Audio.Voice.SherpaOnnx` so bundled Piper models copy from the nupkg (not a sibling checkout).

Restore **`Novolis.Audio.Voice.SherpaOnnx` `2026.1.3+`** from GitHub Packages (model ships as `en-us-piper-amy.zip` and extracts on build). Older `2026.1.2.x` packages have a broken content layout.
