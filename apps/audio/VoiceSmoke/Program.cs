using System.IO.Compression;
using Novolis.Audio.Voice;
using Novolis.Audio.Voice.Atc;
using Novolis.Audio.Voice.SherpaOnnx;

var useNull = args.Contains("--null", StringComparer.OrdinalIgnoreCase);
if (!useNull)
    EnsureBundledModelExtractedFromNuGet();
var writeWav = args.Contains("--wav", StringComparer.OrdinalIgnoreCase);
var speakOnly = args.Contains("--speak-only", StringComparer.OrdinalIgnoreCase);

IVoiceService voice = useNull
    ? new VoiceServiceBuilder().UseNullSynthesizer().UseNullPlayback().BuildService()
    : AtcVoiceProfile.Apply(new VoiceServiceBuilder()).BuildService();

var paths = SherpaVoiceModelPaths.TryResolve(modelDirectory: null, VoiceModelCatalog.EnUsPiperAmy);
Console.WriteLine(paths is null
    ? DescribeMissingModel(AppContext.BaseDirectory)
    : $"Voice: Sherpa model {paths.ProfileId} @ {paths.SampleRateHz} Hz ({paths.ModelDirectory})");

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

static void EnsureBundledModelExtractedFromNuGet()
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

static string DescribeMissingModel(string baseDir)
{
    var profileDir = Path.Combine(baseDir, "models", VoiceModelCatalog.EnUsPiperAmy.Id);
    var flatDir = Path.Combine(baseDir, "models");
    if (!Directory.Exists(flatDir) && !Directory.Exists(profileDir))
        return "Voice: null/silent fallback (no models/ under output — rebuild VoiceSmoke after restore).";

    var onnx = Directory.GetFiles(profileDir, "*.onnx", SearchOption.TopDirectoryOnly)
        .Concat(Directory.Exists(flatDir) ? Directory.GetFiles(flatDir, "*.onnx") : [])
        .FirstOrDefault();
    if (onnx is not null && VoiceModelMaterialization.IsGitLfsPointer(onnx))
        return "Voice: null/silent fallback (ONNX is a Git LFS pointer — run: git lfs pull in novolis-audio).";

    var phontab = Path.Combine(profileDir, "espeak-ng-data", "phontab");
    if (Directory.Exists(phontab) || (File.Exists(phontab) is false && Directory.Exists(Path.Combine(profileDir, "espeak-ng-data"))))
        return "Voice: null/silent fallback (stale/broken model extract — dotnet clean and rebuild VoiceSmoke).";

    return "Voice: null/silent fallback (need models/en-us-piper-amy from Novolis.Audio.Voice.SherpaOnnx on GitHub Packages — restore/build after package upgrade).";
}
