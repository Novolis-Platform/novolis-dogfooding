using Hex1b;
using Hex1b.Widgets;

namespace BridgeCommander.Bridge;

public static class BridgeHexApp
{
    private static readonly string[] Stations = ["helm", "tactical", "engineering", "nav", "comms", "admin"];

    public static Hex1bApp Create(BridgeState state, BridgeCommandService commands) =>
        new(ctx => ctx.VStack(v =>
        [
            v.Border(b =>
            [
                b.Text("— Bridge status —"),
                b.Text(state.ShipName),
                b.Text(state.FormatStatus()),
                b.Text(state.StatusLine)
            ]),

            v.Border(b =>
            [
                b.Text("— Command log —"),
                b.Text("(newest at bottom)"),
                b.List(state.FormatHistory()).Fill()
            ]).Fill(),

            v.Border(b =>
            [
                b.Text("— Command line —"),
                b.HStack(h =>
                [
                    h.Text("Order:"),
                    h.TextBox(state.PromptDraft)
                        .OnTextChanged(e => state.PromptDraft = e.NewText)
                        .Fill(),
                    h.Button("Transmit").OnClick(_ => Transmit(state, commands))
                ]),
                new SeparatorWidget(),
                b.Text($"Active station: {state.ActiveStation}"),
                b.HStack(h => Stations.Select(s =>
                    h.Button(SetLabel(s, state.ActiveStation))
                        .OnClick(_ => SelectStation(state, s))).ToArray())
            ]),

            v.Border(b =>
            [
                b.Text("— Help —"),
                b.Text("helm heading 270 | helm warp 7 | helm full stop"),
                b.Text("tactical lock target | tactical fire | engineering repair"),
                b.Text("belay that | clear queue | repeat last | help"),
                b.Text("Type help or help tactical for full hints in the log.")
            ]),

            v.InfoBar("Tab: focus  |  Transmit  |  pick station  |  Ctrl+C: exit")
        ]));

    private static string SetLabel(string station, string active) =>
        string.Equals(station, active, StringComparison.OrdinalIgnoreCase) ? $"[{station}]" : station;

    private static void SelectStation(BridgeState state, string station)
    {
        state.ActiveStation = station;
        state.StatusLine = $"Switched to {station} station.";
        state.AddHistory(new HistoryEntry(
            DateTimeOffset.UtcNow,
            "",
            HistoryKind.System,
            $"Active station: {station}."));
    }

    private static void Transmit(BridgeState state, BridgeCommandService commands)
    {
        var prompt = state.PromptDraft;
        state.PromptDraft = "";
        _ = commands.SubmitPromptAsync(state, prompt);
    }
}
