using Novolis.Commands.Engine;

namespace BridgeCommander.Bridge;

public sealed class BridgeContextResolver : ICommandContextResolver<BridgeState>
{
    public string? GetActiveContextWord(BridgeState context) => context.ActiveStation;

    public IReadOnlyDictionary<string, string> GetAliases(BridgeState context) =>
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["stop"] = "full stop",
            ["weapons"] = "fire",
            ["lock"] = "lock target"
        };
}
