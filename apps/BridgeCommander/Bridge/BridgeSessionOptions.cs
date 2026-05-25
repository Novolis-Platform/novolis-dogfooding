namespace BridgeCommander.Bridge;

/// <summary>Options for <see cref="BridgeSession.Create"/>.</summary>
public sealed class BridgeSessionOptions
{
    /// <summary>When true, executed orders are spoken via Novolis ATC voice (Sherpa TTS).</summary>
    public bool VoiceEnabled { get; init; } = true;

    /// <summary>Default bridge session (voice on).</summary>
    public static BridgeSessionOptions Default { get; } = new();

    /// <summary>Headless / automation session without TTS playback.</summary>
    public static BridgeSessionOptions WithoutVoice { get; } = new() { VoiceEnabled = false };
}
