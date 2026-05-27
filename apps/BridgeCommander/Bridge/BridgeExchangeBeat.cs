namespace BridgeCommander.Bridge;

/// <summary>One beat in the scripted bridge exchange (display + optional voice + optional order).</summary>
public sealed record BridgeExchangeBeat(
    string Speaker,
    string Display,
    string? VoiceLine = null,
    string? Transmit = null,
    int PauseAfterMs = 400)
{
    /// <summary>Narration-only beat (no command).</summary>
    public static BridgeExchangeBeat Narration(string speaker, string line, int pauseMs = 600) =>
        new(speaker, line, VoiceLine: line, PauseAfterMs: pauseMs);

    /// <summary>Captain order executed on the bridge (spoken, then station response after execute).</summary>
    public static BridgeExchangeBeat Order(string captainLine, string transmit, int pauseMs = 350) =>
        new("Captain", captainLine, VoiceLine: captainLine, Transmit: transmit, PauseAfterMs: pauseMs);
}
