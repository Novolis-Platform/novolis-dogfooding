using Novolis.Commands;
using Novolis.Commands.Engine;
using Novolis.Commands.Queueing;

namespace BridgeCommander.Bridge;

public sealed class BridgeCommandService : IAsyncDisposable
{
    private readonly ICommandEngine<BridgeState> _engine;
    private readonly ICommandQueue _queue;
    private readonly CommandQueueRunner<BridgeState> _runner;
    private readonly CancellationTokenSource _runCts = new();

    public BridgeCommandService(BridgeState state)
    {
        _engine = new CommandEngine<BridgeState>(
            BridgeCommandRegistry.Create(),
            new BridgeContextResolver());

        _queue = new ChannelCommandQueue();
        _runner = new CommandQueueRunner<BridgeState>(_queue, new BridgeCommandProcessor());
        _ = Task.Run(() => _runner.RunAsync(state, _runCts.Token));
    }

    public async Task SubmitPromptAsync(BridgeState state, string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return;

        var trimmed = prompt.Trim();

        if (BridgeHelp.TryGetHelpLines(trimmed, out var helpLines))
        {
            state.AddHistory(new HistoryEntry(
                DateTimeOffset.UtcNow,
                trimmed,
                HistoryKind.Help,
                "Command reference.",
                helpLines));

            state.StatusLine = "Help displayed in command log.";
            return;
        }

        var result = await _engine.ParseCommandAsync(trimmed, state);

        if (result.Success && result.Command is not null)
        {
            state.AddHistory(new HistoryEntry(
                DateTimeOffset.UtcNow,
                trimmed,
                HistoryKind.ParseSuccess,
                $"Queued {result.Command.Name}",
                [$"Priority: {result.Command.Priority}"]));

            await _queue.EnqueueAsync(result.Command);
            return;
        }

        if (result.Failures.Any(f => f.Code == ParseFailureCode.AmbiguousCommand))
        {
            var details = result.Candidates
                .Select(c => $"{c.Name} ({c.Confidence:P0}) — {c.Reason}")
                .ToList();

            state.AddHistory(new HistoryEntry(
                DateTimeOffset.UtcNow,
                trimmed,
                HistoryKind.ParseFailure,
                "Ambiguous command.",
                details));

            state.StatusLine = "Command ambiguous — see log for candidates.";
            return;
        }

        var failure = result.Failures.FirstOrDefault();
        state.AddHistory(new HistoryEntry(
            DateTimeOffset.UtcNow,
            trimmed,
            HistoryKind.ParseFailure,
            failure?.Message ?? "Parse failed.",
            result.Failures.Select(f => $"{f.Code}: {f.Message}").ToList()));

        state.StatusLine = failure?.Message ?? "Unable to parse command.";
    }

    public ValueTask DisposeAsync()
    {
        _runCts.Cancel();
        _runCts.Dispose();
        return ValueTask.CompletedTask;
    }
}
