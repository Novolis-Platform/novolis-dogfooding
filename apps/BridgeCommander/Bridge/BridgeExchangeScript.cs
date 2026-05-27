namespace BridgeCommander.Bridge;

/// <summary>Scripted duty-shift bridge exchange (game-style pacing).</summary>
public static class BridgeExchangeScript
{
    /// <summary>Full voiced patrol engagement sequence.</summary>
    public static IReadOnlyList<BridgeExchangeBeat> PatrolEngagement { get; } =
    [
        BridgeExchangeBeat.Narration(
            "Computer",
            "Bridge to all stations. USS Novolis on patrol, sector seven alpha. All departments report ready.",
            pauseMs: 900),

        BridgeExchangeBeat.Narration(
            "Executive Officer",
            "Captain, long-range sensors show a hostile frigate on bearing two seven zero. Recommend weapons tight and shields up.",
            pauseMs: 700),

        BridgeExchangeBeat.Order(
            "Helm, come right to heading zero nine zero, steady as she goes.",
            "helm, set heading to 090"),

        BridgeExchangeBeat.Order(
            "Tactical, lock the hostile and stand by phasers.",
            "tactical, lock target"),

        BridgeExchangeBeat.Order(
            "Engineering, divert auxiliary power to the forward shield grid.",
            "engineering, divert shields"),

        BridgeExchangeBeat.Order(
            "Tactical, fire when ready!",
            "tactical, fire"),

        BridgeExchangeBeat.Order(
            "Helm, all ahead full. Let's close the distance.",
            "helm, all ahead full"),

        BridgeExchangeBeat.Order(
            "Comms, open hail on priority channel. Identify us as Federation patrol.",
            "comms, hail"),

        BridgeExchangeBeat.Order(
            "Navigation, plot jump solution to waypoint alpha for after action.",
            "nav, set course waypoint alpha"),

        BridgeExchangeBeat.Order(
            "Helm, come about to face the debris field. Prepare for recovery ops.",
            "helm, come about"),

        BridgeExchangeBeat.Narration(
            "Computer",
            "Hostile neutralized. Hull integrity ninety two percent. Standing by for further orders.",
            pauseMs: 800),

        BridgeExchangeBeat.Narration(
            "Captain",
            "Well done, all stations. Log the engagement and resume patrol. Bridge out.",
            pauseMs: 500),
    ];
}
