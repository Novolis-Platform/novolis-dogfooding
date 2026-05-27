using Novolis.Audio.Voice;
using Novolis.Audio.Voice.Atc;

namespace BridgeCommander.Bridge;

/// <summary>ATC TTS for bridge lines (GPR <c>Novolis.Audio.Voice*</c> packages).</summary>
public static class BridgeVoice
{
    /// <summary>Default urgent ATC profile for in-game bridge audio.</summary>
    public static AtcVoiceOptions UrgentAtcProfile { get; } = new()
    {
        SpeakingRate = 1.16f,
        Drive = 3f,
        OutputGainDb = 5.5f,
        HissLevel = 0.0045f,
    };

    /// <summary>Creates an ATC-configured voice service, or null when disabled.</summary>
    public static IVoiceService? CreateService(bool enabled, AtcVoiceOptions? profile = null)
    {
        if (!enabled)
            return null;

        BridgeVoiceBootstrap.EnsureBundledModelExtracted();
        return AtcVoiceProfile.Apply(new VoiceServiceBuilder(), profile ?? UrgentAtcProfile).BuildService();
    }

    /// <summary>Speaks narrative or station text (not status-prefixed).</summary>
    public static async Task SpeakLineAsync(
        IVoiceService? voice,
        string text,
        CancellationToken cancellationToken = default)
    {
        if (voice is null || string.IsNullOrWhiteSpace(text))
            return;

        try
        {
            await voice.SpeakAsync(text.Trim(), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Spectre.Console.AnsiConsole.MarkupLine(
                $"[red][[bridge-voice]][/] {Spectre.Console.Markup.Escape(ex.Message)}");
        }
    }
}
