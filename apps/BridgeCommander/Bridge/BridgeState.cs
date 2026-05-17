using Novolis.Commands;

namespace BridgeCommander.Bridge;

public sealed class BridgeState
{
    public const int MaxHistory = 200;

    public string ShipName { get; } = "USS Novolis";
    public string CaptainName { get; } = "Captain";
    public string ActiveStation { get; set; } = "helm";
    public int Heading { get; set; } = 180;
    public int SpeedWarp { get; set; } = 5;
    public int ShieldPercent { get; set; } = 80;
    public int HullPercent { get; set; } = 100;
    public bool TargetLocked { get; set; }
    public string? TargetName { get; set; }
    public string StatusLine { get; set; } = "Standing by.";
    public string? LastExecutedCommand { get; set; }
    public CommandEnvelope? LastEnvelope { get; set; }

    public List<HistoryEntry> History { get; } = [];

    public string PromptDraft { get; set; } = "";

    public void AddHistory(HistoryEntry entry)
    {
        History.Add(entry);
        if (History.Count > MaxHistory)
            History.RemoveAt(0);
    }

    public IReadOnlyList<string> FormatHistory()
    {
        var lines = new List<string>();
        foreach (var entry in History)
        {
            lines.Add(entry.DisplayLine);
            if (entry.Details is not { Count: > 0 })
                continue;

            foreach (var detail in entry.Details)
                lines.Add(string.IsNullOrEmpty(detail) ? "" : $"  {detail}");
        }

        return lines;
    }

    public string FormatStatus() =>
        $"Station: {ActiveStation.ToUpperInvariant()}  |  HDG {Heading,3}°  |  Warp {SpeedWarp}  |  " +
        $"Shields {ShieldPercent}%  |  Hull {HullPercent}%  |  Target: {(TargetLocked ? TargetName ?? "locked" : "none")}";
}
