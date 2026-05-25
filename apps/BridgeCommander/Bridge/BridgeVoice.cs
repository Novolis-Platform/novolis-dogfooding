using Novolis.Audio.Voice;
using Novolis.Audio.Voice.Atc;
using Novolis.Audio.Voice.SherpaOnnx;

namespace BridgeCommander.Bridge;

/// <summary>ATC TTS for bridge status lines (GPR <c>Novolis.Audio.Voice*</c> packages).</summary>
public static class BridgeVoice
{
    /// <summary>Creates an ATC-configured voice service, or null when disabled.</summary>
    public static IVoiceService? CreateService(bool enabled)
    {
        if (!enabled)
            return null;

        ConfigureBundledModelFromOutput();
        return AtcVoiceProfile.Apply(new VoiceServiceBuilder()).BuildService();
    }

    /// <summary>
    /// Points <see cref="SherpaVoiceModelPaths"/> at bundled content under the app output
    /// (GPR nupkg uses <c>models/</c> or <c>models/en-us-piper-amy/</c>).
    /// </summary>
    public static void ConfigureBundledModelFromOutput()
    {
        var dir = FindBundledModelsDirectory();
        if (dir is not null)
            Environment.SetEnvironmentVariable(SherpaVoiceModelPaths.EnvModelDirectory, dir);
    }

    internal static string? FindBundledModelsDirectory()
    {
        foreach (var root in GetSearchRoots())
        {
            foreach (var sub in new[] { "en-us-piper-amy", "" })
            {
                var models = string.IsNullOrEmpty(sub)
                    ? Path.Combine(root, "models")
                    : Path.Combine(root, "models", sub);
                if (File.Exists(Path.Combine(models, "tokens.txt")))
                    return models;
            }
        }

        return null;
    }

    private static IEnumerable<string> GetSearchRoots()
    {
        yield return AppContext.BaseDirectory;
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 6 && dir?.Parent is not null; i++, dir = dir.Parent)
            yield return dir.FullName;
    }

    /// <summary>Speaks <paramref name="text"/> without blocking the command queue on failure.</summary>
    public static void Announce(IVoiceService? voice, string? text)
    {
        if (voice is null || string.IsNullOrWhiteSpace(text))
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await voice.SpeakAsync(text).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[bridge-voice] {ex.Message}");
            }
        });
    }
}
