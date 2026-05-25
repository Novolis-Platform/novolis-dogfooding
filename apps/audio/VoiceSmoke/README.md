# VoiceSmoke

Dogfoods **Novolis.Audio.Voice** from GitHub Packages only (`2026.1.*`).

```bash
dotnet restore
dotnet run --project apps/audio/VoiceSmoke
dotnet run --project apps/audio/VoiceSmoke -- --null
dotnet run --project apps/audio/VoiceSmoke -- --calm
```

Default profile is **urgent ATC radio**: faster speaking rate (`1.18x`), band-limited bandwidth, compression/drive, gain, and light channel hiss (`Novolis.Audio.Voice.Atc` **2026.1.5+**). Use `--calm` for the older dry Piper delivery.

Requires a direct `PackageReference` to `Novolis.Audio.Voice.SherpaOnnx` so bundled Piper models copy from the nupkg (not a sibling checkout).

Restore **`Novolis.Audio.Voice.SherpaOnnx` `2026.1.3+`** from GitHub Packages (model ships as `en-us-piper-amy.zip` and extracts on build). Older `2026.1.2.x` packages have a broken content layout.
