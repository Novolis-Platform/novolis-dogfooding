using System.IO.Compression;
using Novolis.Audio.Voice;
using Novolis.Audio.Voice.SherpaOnnx;

namespace BridgeCommander.Bridge;

/// <summary>Ensures bundled Piper model content is extracted from the SherpaOnnx nupkg.</summary>
internal static class BridgeVoiceBootstrap
{
    public static void EnsureBundledModelExtracted()
    {
        var baseDir = AppContext.BaseDirectory;
        var profileDir = Path.Combine(baseDir, "models", VoiceModelCatalog.EnUsPiperAmy.Id);
        var zipPath = Path.Combine(baseDir, "models", $"{VoiceModelCatalog.EnUsPiperAmy.Id}.zip");
        var phontab = Path.Combine(profileDir, "espeak-ng-data", "phontab");

        if (File.Exists(Path.Combine(profileDir, "tokens.txt")) && File.Exists(phontab))
            return;

        if (!File.Exists(zipPath))
            return;

        if (Directory.Exists(profileDir))
            Directory.Delete(profileDir, recursive: true);

        Directory.CreateDirectory(profileDir);
        ZipFile.ExtractToDirectory(zipPath, profileDir);
    }
}
