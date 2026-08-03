namespace PolityTriad;

/// <summary>Rolling month series for dashboard sparklines / headless arcs.</summary>
sealed class TriadHistory
{
    public List<double> AlphaLegitimacy { get; } = [];
    public List<double> AlphaApproval { get; } = [];
    public List<double> AlphaWarFatigue { get; } = [];
    public List<double> AlphaStateCash { get; } = [];
    public List<double> AlphaGdp { get; } = [];
    public List<double> BetaWarFatigue { get; } = [];
    public List<double> BetaLegitimacy { get; } = [];
    public List<double> TradeVolume { get; } = [];
    public List<int> Battles { get; } = [];
    public List<string> Phases { get; } = [];

    public void Record(
        double aLeg, double aApp, double aWf, double aCash, double aGdp,
        double bLeg, double bWf, double trade, int battles, string phase)
    {
        AlphaLegitimacy.Add(aLeg);
        AlphaApproval.Add(aApp);
        AlphaWarFatigue.Add(aWf);
        AlphaStateCash.Add(aCash);
        AlphaGdp.Add(aGdp);
        BetaLegitimacy.Add(bLeg);
        BetaWarFatigue.Add(bWf);
        TradeVolume.Add(trade);
        Battles.Add(battles);
        Phases.Add(phase);
    }

    public static string Spark(IReadOnlyList<double> series, int width = 24)
    {
        if (series.Count == 0)
            return "";
        const string blocks = "▁▂▃▄▅▆▇█";
        var take = series.Count <= width ? series : series.Skip(series.Count - width).ToList();
        var min = take.Min();
        var max = take.Max();
        var span = Math.Max(1e-9, max - min);
        return string.Concat(take.Select(v =>
        {
            var t = (v - min) / span;
            var i = (int)Math.Round(t * (blocks.Length - 1));
            return blocks[Math.Clamp(i, 0, blocks.Length - 1)];
        }));
    }
}
