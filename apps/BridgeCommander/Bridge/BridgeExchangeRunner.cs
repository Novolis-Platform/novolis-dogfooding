namespace BridgeCommander.Bridge;

/// <summary>Runs the scripted bridge exchange with Spectre output and awaited voice.</summary>
public static class BridgeExchangeRunner
{
    public static async Task RunAsync(
        BridgeSession session,
        IReadOnlyList<BridgeExchangeBeat>? script = null,
        CancellationToken cancellationToken = default)
    {
        script ??= BridgeExchangeScript.PatrolEngagement;
        var voice = session.Voice;

        BridgeSpectreUi.ShowTitle();
        BridgeSpectreUi.RenderBridge(session.State);

        foreach (var beat in script)
        {
            cancellationToken.ThrowIfCancellationRequested();
            BridgeSpectreUi.ShowBeat(beat);

            if (beat.VoiceLine is not null)
                await BridgeVoice.SpeakLineAsync(voice, beat.VoiceLine, cancellationToken).ConfigureAwait(false);

            if (beat.Transmit is not null)
            {
                await session.TransmitAsync(beat.Transmit, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                await session.Announcer.AnnounceAsync(session.State.StatusLine, cancellationToken)
                    .ConfigureAwait(false);
                BridgeSpectreUi.RenderBridge(session.State);
            }

            if (beat.PauseAfterMs > 0)
                await Task.Delay(beat.PauseAfterMs, cancellationToken).ConfigureAwait(false);
        }

        BridgeSpectreUi.ShowClosing();
    }
}
