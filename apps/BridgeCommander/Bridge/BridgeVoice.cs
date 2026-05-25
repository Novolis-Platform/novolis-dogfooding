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

        return AtcVoiceProfile.Apply(new VoiceServiceBuilder()).BuildService();
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
