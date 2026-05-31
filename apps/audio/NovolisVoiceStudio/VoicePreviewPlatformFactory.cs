using Novolis.Audio.Voice;
using Novolis.Audio.Voice.Design;
using Novolis.Audio.Voice.Phraseology;
using Novolis.Audio.Voice.Platform;
using Novolis.Audio.Voice.Platform.Windows;

namespace NovolisVoiceStudio;

internal static class VoicePreviewPlatformFactory
{
    public static IVoiceService Create(VoicePresetDraft draft)
    {
        Func<string, string>? normalize = null;
        if (draft.UsePhraseology)
        {
            var phraseology = new DefaultPhraseologyNormalizer();
            normalize = phraseology.Normalize;
        }

        return new WindowsPlatformVoiceService(draft.Platform ?? new PlatformSpeechOptions(), normalize);
    }
}
