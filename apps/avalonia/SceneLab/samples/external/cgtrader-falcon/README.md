# CGTrader Falcon drop (local dogfood)

Source listing: https://www.cgtrader.com/free-3d-models/space/spaceship/27-5millennium-falcon-star-wars-spacecraft  
Creator: **evercity** · Model ID `#4201368` · Royalty Free (per CGTrader listing)

## Steps

1. Log in on CGTrader and **Free Download** (FBX preferred; BLEND/MAX also fine if you re-export).
2. Unzip into this folder (keep `.fbx` / `.obj` here or in a subfolder).
3. Bake into SceneLab:

```powershell
dotnet run --project novolis-dogfooding/apps/avalonia/SceneLab/tools/CorellianFreighterBuilder `
  -p:NovolisUseProjectReferences=true -- --import
```

Or pass an explicit path:

```powershell
dotnet run --project …/CorellianFreighterBuilder -p:NovolisUseProjectReferences=true -- `
  --import "D:\path\to\falcon.fbx" `
  novolis-dogfooding/apps/avalonia/SceneLab/samples/corellian-freighter.nov3djson
```

4. Open in SceneLab (HTTP session):

```powershell
Invoke-RestMethod http://127.0.0.1:18785/session/command -Method Post `
  -ContentType application/json `
  -Body '{"actionId":"open","path":"d:/novolis/novolis-dogfooding/apps/avalonia/SceneLab/samples/corellian-freighter.nov3djson"}'
Invoke-RestMethod http://127.0.0.1:18785/session/command -Method Post `
  -ContentType application/json -Body '{"actionId":"fit"}'
```

Mesh is scaled so longest axis ≈ **34.37 m** (Haynes OAL). Geometry only (wireframe SceneLab); 8K PBR maps are not loaded.
