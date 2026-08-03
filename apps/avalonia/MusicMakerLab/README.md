# MusicMakerLab

Magix Music Maker–style **Arrangement** plus an **orchestral multi-part score**.

## Arrangement demo cue

On launch, Arrangement loads the first **~20 seconds** of **By The Sword** by **Kevin Graham**
(public preview from the composer’s [Ablaze](https://www.kevingrahamcomposer.com/ablaze) page),
cached under `%LOCALAPPDATA%\Novolis\MusicMakerLab\`.

For production/commercial use, license the track via Artlist (or your deal with the composer)—the preview is for local dogfood listening only.

## Run

```powershell
dotnet run --project d:\novolis\novolis-dogfooding\apps\avalonia\MusicMakerLab\MusicMakerLab.csproj -p:NovolisUseProjectReferences=true
```
