namespace BridgeCommander.Bridge;

public static class BridgeHelp
{
    public static bool TryGetHelpLines(string prompt, out IReadOnlyList<string> lines)
    {
        if (!prompt.Trim().Equals("help", StringComparison.OrdinalIgnoreCase) &&
            !prompt.Trim().StartsWith("help ", StringComparison.OrdinalIgnoreCase))
        {
            lines = [];
            return false;
        }

        var topic = prompt.Trim().Length > 4
            ? prompt.Trim()[5..].Trim().ToLowerInvariant()
            : null;

        lines = topic switch
        {
            null or "" => GeneralHelp(),
            "helm" => HelmHelp(),
            "tactical" => TacticalHelp(),
            "engineering" => EngineeringHelp(),
            "nav" => NavHelp(),
            "comms" => CommsHelp(),
            "admin" => AdminHelp(),
            "system" or "builtins" => SystemHelp(),
            _ => [$"Unknown help topic '{topic}'. Try: help helm | tactical | engineering | nav | comms | admin | system"]
        };

        return true;
    }

    private static IReadOnlyList<string> GeneralHelp() =>
    [
        "Bridge Commander — type an order, press Transmit.",
        "",
        "Format: [station] <verb> [arguments]   — or omit station to use the active one.",
        "Active station is shown below the command line; click a station button to switch.",
        "",
        "── Helm ──",
        ..HelmHelp().Skip(1),
        "",
        "── Tactical ──",
        ..TacticalHelp().Skip(1),
        "",
        "── Engineering ──",
        ..EngineeringHelp().Skip(1),
        "",
        "── Nav / Comms / Admin ──",
        ..NavHelp().Skip(1),
        ..CommsHelp().Skip(1),
        ..AdminHelp().Skip(1),
        "",
        "── System ──",
        ..SystemHelp().Skip(1),
        "",
        "── Demos ──",
        "fire                    → ambiguous (admin dismiss vs tactical weapons)",
        "help <station>          → this page, filtered by station",
        "",
        "Type help helm (etc.) for one station only."
    ];

    private static IReadOnlyList<string> HelmHelp() =>
    [
        "Helm commands (station: helm):",
        "helm heading 270        → course 270°; status shows new heading after ~1s",
        "helm set heading 270    → same as above",
        "helm warp 7             → speed warp 7 (0–9)",
        "helm full stop          → warp 0, all stop",
        "heading 270             → works when helm is the active station"
    ];

    private static IReadOnlyList<string> TacticalHelp() =>
    [
        "Tactical commands (station: tactical):",
        "tactical lock target    → locks hostile KR-12; required before firing",
        "tactical fire           → fires weapons if target locked (~2.5s); else 'no target lock'",
        "tactical fire weapons   → same as fire",
        "fire                    → ambiguous without station — try help for demo"
    ];

    private static IReadOnlyList<string> EngineeringHelp() =>
    [
        "Engineering commands (station: engineering):",
        "engineering divert shields  → shields +15% (caps 100)",
        "engineering divert weapons  → shields −10%, power to weapons",
        "engineering repair          → hull +8% (~3s)"
    ];

    private static IReadOnlyList<string> NavHelp() =>
    [
        "Nav commands (station: nav):",
        "nav set course alpha centauri   → logs course laid in (~1.5s)",
        "nav course mars                 → same (destination is free text)"
    ];

    private static IReadOnlyList<string> CommsHelp() =>
    [
        "Comms commands (station: comms):",
        "comms hail              → hail transmitted on open channel"
    ];

    private static IReadOnlyList<string> AdminHelp() =>
    [
        "Admin commands (station: admin):",
        "admin fire              → personnel transfer logged (used in ambiguity demo)"
    ];

    private static IReadOnlyList<string> SystemHelp() =>
    [
        "System commands (no station prefix):",
        "belay that              → emergency interrupt; cancels in-flight command",
        "clear queue             → dismiss queued orders (not yet drained in v0 UI)",
        "repeat last             → re-runs last successful command",
        "help                    → this help",
        "help tactical           → help for one station"
    ];
}
