using BridgeCommander.Bridge;
using Hex1b;

var state = new BridgeState();
state.HelpPanelLines.AddRange(BridgeHelp.GetLines(null));
state.AddHistory(new HistoryEntry(
    DateTimeOffset.UtcNow,
    "",
    HistoryKind.System,
    "Bridge online. Prefix every order with a station (helm, tactical, weaps…)."));

await using var commands = new BridgeCommandService(state);

var app = BridgeHexApp.Create(state, commands);
await app.RunAsync();
