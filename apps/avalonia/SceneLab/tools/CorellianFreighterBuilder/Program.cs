using CorellianFreighterBuilder;

var samples = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "samples"));
var stageDir = Path.Combine(samples, "freighter-stages");
var finalPath = Path.Combine(samples, "corellian-freighter.nov3djson");
var dropDir = Path.Combine(samples, "external", "cgtrader-falcon");

if (args.Length > 0 && args[0] is "--import" or "import")
{
    // Usage: --import <fbx|obj|…> [out.nov3djson]
    var meshPath = args.Length > 1
        ? args[1]
        : FindDropMesh(dropDir)
          ?? throw new InvalidOperationException(
              $"No mesh path given and nothing in drop folder:\n  {dropDir}\n" +
              "Download FBX from CGTrader (login required), unzip into that folder, re-run.");

    var outPath = args.Length > 2 ? args[2] : finalPath;
    Console.WriteLine($"Import → {meshPath}");
    Console.WriteLine("Source: CGTrader evercity #4201368 — local dogfood; respect listing RF license.");
    var mesh = ExternalMeshImport.Load(meshPath, targetLengthMeters: 34.37f);
    ExternalMeshImport.WriteScene(mesh, outPath, "YT-1300 (CGTrader evercity import)");
    return;
}

if (args.Length > 0)
    stageDir = args[0];
if (args.Length > 1)
    finalPath = args[1];

Console.WriteLine($"Stages → {stageDir}");
Console.WriteLine("Homage: procedural CEC YT-1300 landmarks (Haynes) — original mesh, not a licensed asset.");
Console.WriteLine("Tip: --import <path-to.fbx> to bake the CGTrader evercity Falcon instead.");
FreighterStages.BuildAll(stageDir, finalPath);

static string? FindDropMesh(string dir)
{
    if (!Directory.Exists(dir))
        return null;
    string[] exts = [".fbx", ".obj", ".gltf", ".glb", ".dae", ".3ds"];
    foreach (var ext in exts)
    {
        var hit = Directory.EnumerateFiles(dir, "*" + ext, SearchOption.AllDirectories)
            .OrderByDescending(f => new FileInfo(f).Length)
            .FirstOrDefault();
        if (hit is not null)
            return hit;
    }

    return null;
}
