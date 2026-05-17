using Hex1b;
using Hex1b.Widgets;

namespace BridgeCommander.Bridge;

public static class BridgeHexApp
{
    public static Hex1bApp Create(BridgeState state, BridgeCommandService commands) =>
        new(ctx => ctx.VStack(v =>
        [
            v.Border(b =>
            [
                b.Text(state.ShipName),
                b.Text(state.FormatStatus()),
                b.Text(state.StatusLine)
            ]),

            v.Border(b =>
            [
                b.Text("Command log"),
                b.List(state.FormatHistory()).Fill()
            ]).Fill(),

            v.Border(b =>
            [
                b.Text("Reference"),
                b.List(state.HelpPanelLines).Fill()
            ]).Fill(),

            v.Border(b =>
            [
                b.HStack(h =>
                [
                    h.Text("Order:"),
                    h.TextBox(state.PromptDraft)
                        .OnTextChanged(e => state.PromptDraft = e.NewText)
                        .Fill(),
                    h.Button("Transmit").OnClick(_ => Transmit(state, commands))
                ])
            ]),

            v.InfoBar("Prefix required (helm, weaps, pilot…)  |  help  |  belay that  |  Ctrl+C: exit")
        ]));

    private static void Transmit(BridgeState state, BridgeCommandService commands)
    {
        var prompt = state.PromptDraft;
        state.PromptDraft = "";
        _ = TransmitAsync(state, commands, prompt);
    }

    private static async Task TransmitAsync(BridgeState state, BridgeCommandService commands, string prompt) =>
        await commands.SubmitPromptAsync(state, prompt);
}
