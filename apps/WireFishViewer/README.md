# WireFishViewer

Avalonia WireShark-style packet viewer dogfooding **Novolis.Transports.WireFish**, **Novolis.Messaging.Channels**, **Novolis.Avalonia.Controls**, and **Novolis.Avalonia.Layout**.

Requires **Npcap** for live capture. The app relaunches elevated (UAC) when needed for capture driver access.

## Run

```powershell
cd novolis-dogfooding
dotnet run --project apps/WireFishViewer
```

Toolbar: **Start Npcap** (if driver stopped), pick interface, **Start** / **Stop** capture. Packet list and detail panes update as frames arrive.

No `--smoke` CLI flag. Unit tests live in `apps/WireFishViewer.Tests`.

## ProjectRef note

For local iteration against sibling repos, open `Novolis.Platform.slnx` or pass `-p:NovolisUseProjectReferences=true`. Committed `.csproj` files use `PackageReference` from GitHub Packages only.
