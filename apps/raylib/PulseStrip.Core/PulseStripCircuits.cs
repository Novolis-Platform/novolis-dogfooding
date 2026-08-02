namespace PulseStrip.Core;

using Novolis.Simulation.Racing.Tracks;

/// <summary>Catalog of spline circuits used by PulseStrip.</summary>
public static class PulseStripCircuits
{
    public static IReadOnlyList<(string DisplayName, ITrackDefinition Definition)> All { get; } =
    [
        ("Pulse Oval", BuiltInTracks.CompactOval),
        ("Neon Esses", BuiltInTracks.EssesCircuit),
    ];

    public static ITrackDefinition ByIndex(int index)
    {
        if (index < 0 || index >= All.Count)
            return All[0].Definition;
        return All[index].Definition;
    }

    public static string DisplayName(int index)
    {
        if (index < 0 || index >= All.Count)
            return All[0].DisplayName;
        return All[index].DisplayName;
    }
}
