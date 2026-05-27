using Novolis.Audio.Voice.Atc;

namespace BridgeCommander.Bridge;

/// <summary>Options for <see cref="BridgeSession.Create"/>.</summary>
public sealed class BridgeSessionOptions
{
    /// <summary>When true, executed orders are spoken via Novolis ATC voice (Sherpa TTS).</summary>
    public bool VoiceEnabled { get; init; } = true;

    /// <summary>When true, <see cref="BridgeVoiceAnnouncer"/> awaits each line (exchange / interactive).</summary>
    public bool AwaitVoicePlayback { get; init; }

    /// <summary>ATC voice profile; null uses <see cref="BridgeVoice.UrgentAtcProfile"/>.</summary>
    public AtcVoiceOptions? VoiceProfile { get; init; }

    /// <summary>Default bridge session (voice on, async playback for MCP compatibility).</summary>
    public static BridgeSessionOptions Default { get; } = new();

    /// <summary>Headless / automation session without TTS playback.</summary>
    public static BridgeSessionOptions WithoutVoice { get; } = new() { VoiceEnabled = false };

    /// <summary>Scripted Spectre exchange with blocking voice playback.</summary>
    public static BridgeSessionOptions ForExchange { get; } = new()
    {
        VoiceEnabled = true,
        AwaitVoicePlayback = true,
    };

    /// <summary>Interactive Spectre console with blocking voice.</summary>
    public static BridgeSessionOptions ForInteractive { get; } = new()
    {
        VoiceEnabled = true,
        AwaitVoicePlayback = true,
    };
}
