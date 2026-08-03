namespace MobilityLab.Experiment;

/// <summary>Fixed treatment/control parameters for the tax–mobility experiment.</summary>
readonly record struct ExperimentSpec(
    double AlphaTax,
    double BetaTax,
    double GammaTax,
    int Months,
    int Seed,
    bool WarShockOn,
    bool AgentsEnabled)
{
    public static ExperimentSpec Default { get; } = new(
        AlphaTax: 0.38,
        BetaTax: 0.14,
        GammaTax: 0.12,
        Months: 36,
        Seed: 42,
        WarShockOn: false,
        AgentsEnabled: false);
}
