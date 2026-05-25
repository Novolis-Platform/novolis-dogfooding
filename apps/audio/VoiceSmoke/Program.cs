using Novolis.Audio.Voice;
using Novolis.Audio.Voice.Atc;
using Novolis.Audio.Voice.SherpaOnnx;

var useNull = args.Contains("--null", StringComparer.OrdinalIgnoreCase);
if (!useNull)
    ConfigureBundledModelFromOutput();
var writeWav = args.Contains("--wav", StringComparer.OrdinalIgnoreCase);
var speakOnly = args.Contains("--speak-only", StringComparer.OrdinalIgnoreCase);

IVoiceService voice = useNull
    ? new VoiceServiceBuilder().UseNullSynthesizer().UseNullPlayback().BuildService()
    : AtcVoiceProfile.Apply(new VoiceServiceBuilder()).BuildService();

var paths = SherpaVoiceModelPaths.TryResolve(modelDirectory: null, VoiceModelCatalog.EnUsPiperAmy);
Console.WriteLine(paths is null || !VoiceModelMaterialization.IsMaterializedOnnx(paths.ModelFile)
    ? "Voice: null/silent fallback (model not materialized — run git lfs pull on novolis-audio or use GPR nupkg content)."
    : $"Voice: Sherpa model {paths.ProfileId} @ {paths.SampleRateHz} Hz");

var samples = new[]
{
    "Tower, ready for departure.",
    "Comms: hail transmitted on open channel.",
    "SAS one two three climbing flight level three five zero.",
};

foreach (var text in samples)
{
    Console.WriteLine($"Speak: {text}");
    await voice.SpeakAsync(text);
}

if (writeWav && !speakOnly)
{
    var wavPath = Path.Combine(Path.GetTempPath(), "novolis-voice-smoke.wav");
    await voice.WriteToFileAsync(samples[^1], new FileInfo(wavPath));
    Console.WriteLine($"Wrote {wavPath}");
}

Console.WriteLine("VoiceSmoke OK");
return 0;

static void ConfigureBundledModelFromOutput()
{
    foreach (var root in GetSearchRoots())
    {
        foreach (var sub in new[] { "en-us-piper-amy", "" })
        {
            var models = string.IsNullOrEmpty(sub)
                ? Path.Combine(root, "models")
                : Path.Combine(root, "models", sub);
            if (!File.Exists(Path.Combine(models, "tokens.txt")))
                continue;

            Environment.SetEnvironmentVariable(SherpaVoiceModelPaths.EnvModelDirectory, models);
            return;
        }
    }
}

static IEnumerable<string> GetSearchRoots()
{
    yield return AppContext.BaseDirectory;
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    for (var i = 0; i < 6 && dir?.Parent is not null; i++, dir = dir.Parent)
        yield return dir.FullName;
}
