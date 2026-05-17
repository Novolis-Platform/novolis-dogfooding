using BridgeCommander.Bridge;
using Hex1b;

var state = new BridgeState();
state.AddHistory(new HistoryEntry(
    DateTimeOffset.UtcNow,
    "",
    HistoryKind.System,
    "Bridge command interface online.",
    ["Novolis.Commands dogfood — parse, queue, execute.", "Type help for commands and expected results."]));

await using var commands = new BridgeCommandService(state);

var app = BridgeHexApp.Create(state, commands);
await app.RunAsync();
