using Novolis.Snapshots;

namespace MeshBench.Services;

/// <summary>RGB triple for git-graph UI brushes.</summary>
public readonly record struct GraphRgb(byte R, byte G, byte B)
{
    public static GraphRgb FromArgb(byte r, byte g, byte b) => new(r, g, b);
}

/// <summary>Stable colors for branches and snapshot kinds.</summary>
internal static class GitGraphPalette
{
    private static readonly GraphRgb[] BranchHues =
    [
        GraphRgb.FromArgb(90, 170, 255),
        GraphRgb.FromArgb(255, 140, 90),
        GraphRgb.FromArgb(140, 220, 120),
        GraphRgb.FromArgb(220, 120, 200),
        GraphRgb.FromArgb(255, 210, 80),
        GraphRgb.FromArgb(120, 200, 200),
        GraphRgb.FromArgb(200, 160, 255),
        GraphRgb.FromArgb(255, 120, 120),
    ];

    public static GraphRgb BranchColor(string branchName)
    {
        if (branchName.Equals("main", StringComparison.OrdinalIgnoreCase))
            return GraphRgb.FromArgb(90, 170, 255);

        var hash = StableHash(branchName);
        return BranchHues[Math.Abs(hash) % BranchHues.Length];
    }

    public static GraphRgb KindColor(string kind) =>
        kind.ToLowerInvariant() switch
        {
            SnapshotKinds.Manual => GraphRgb.FromArgb(100, 210, 120),
            SnapshotKinds.Safety => GraphRgb.FromArgb(255, 190, 90),
            SnapshotKinds.Autosave => GraphRgb.FromArgb(150, 150, 160),
            SnapshotKinds.Quick => GraphRgb.FromArgb(90, 200, 220),
            SnapshotKinds.ExportCheckpoint => GraphRgb.FromArgb(180, 150, 255),
            _ => GraphRgb.FromArgb(180, 180, 190),
        };

    public static string KindLabel(string kind) =>
        string.IsNullOrWhiteSpace(kind) ? "save" : kind;

    private static int StableHash(string text)
    {
        var hash = 17;
        foreach (var ch in text)
            hash = (hash * 31) + ch;
        return hash;
    }
}
