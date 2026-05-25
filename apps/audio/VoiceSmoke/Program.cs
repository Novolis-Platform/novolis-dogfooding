using Novolis.Audio.Voice;
using Novolis.Audio.Voice.Atc;
using Novolis.Audio.Voice.SherpaOnnx;

var useNull = args.Contains("--null", StringComparer.OrdinalIgnoreCase);
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
