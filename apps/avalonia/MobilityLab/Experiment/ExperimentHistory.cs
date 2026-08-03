using System.Globalization;
using System.Text;

namespace MobilityLab.Experiment;

sealed class ExperimentHistory
{
    public List<MonthSample> Months { get; } = [];

    public void Record(MonthSample sample) => Months.Add(sample);

    public ExperimentResult Evaluate(TaxMobilityWorld.Model model)
    {
        var checks = BuildCouplingChecks(model);
        var last = Months.Count > 0 ? Months[^1] : null;
        var first = Months.Count > 0 ? Months[0] : null;
        return new ExperimentResult
        {
            Spec = model.Spec,
            SampleCount = Months.Count,
            AlphaPopStart = first?.Alpha.Population ?? 0,
            AlphaPopEnd = last?.Alpha.Population ?? 0,
            BetaPopStart = first?.Beta.Population ?? 0,
            BetaPopEnd = last?.Beta.Population ?? 0,
            GammaPopStart = first?.Gamma.Population ?? 0,
            GammaPopEnd = last?.Gamma.Population ?? 0,
            AlphaNetMigrationSum = Months.Sum(m => m.Alpha.NetMigration),
            AlphaPeakPressure = Months.Count == 0 ? 0 : Months.Max(m => m.Alpha.EmigrationPressure),
            BetaPeakPressure = Months.Count == 0 ? 0 : Months.Max(m => m.Beta.EmigrationPressure),
            AlphaLegitimacyEnd = last?.Alpha.Legitimacy ?? 0,
            BetaLegitimacyEnd = last?.Beta.Legitimacy ?? 0,
            PopulationMigrated = model.Telemetry.PopulationMigrated,
            Checks = checks,
        };
    }

    List<CouplingCheck> BuildCouplingChecks(TaxMobilityWorld.Model model)
    {
        var inv = CultureInfo.InvariantCulture;
        var checks = new List<CouplingCheck>();
        if (Months.Count < 2)
        {
            checks.Add(new CouplingCheck(false, "horizon", "Need ≥2 month samples"));
            return checks;
        }

        var first = Months[0];
        var last = Months[^1];
        var alphaNet = Months.Sum(m => m.Alpha.NetMigration);
        var alphaPopDrop = last.Alpha.Population < first.Alpha.Population;
        var peakA = Months.Max(m => m.Alpha.EmigrationPressure);
        var peakB = Months.Max(m => m.Beta.EmigrationPressure);
        var meanPushA = Months.Average(m => m.Alpha.EmigrationPressure);
        var meanPushB = Months.Average(m => m.Beta.EmigrationPressure);

        checks.Add(new CouplingCheck(
            alphaNet < -10_000 || alphaPopDrop,
            "α pop outflow",
            $"α net migration Σ={alphaNet.ToString("0", inv)}; " +
            $"pop {first.Alpha.Population.ToString("0", inv)}→{last.Alpha.Population.ToString("0", inv)}"));

        checks.Add(new CouplingCheck(
            peakA > peakB && meanPushA > meanPushB,
            "α pressure > β",
            $"peak α {peakA.ToString("0.00", inv)} vs β {peakB.ToString("0.00", inv)}; " +
            $"mean α {meanPushA.ToString("0.00", inv)} vs β {meanPushB.ToString("0.00", inv)}"));

        checks.Add(new CouplingCheck(
            last.Alpha.Legitimacy < last.Beta.Legitimacy,
            "α L < β L",
            $"end legitimacy α {last.Alpha.Legitimacy.ToString("0.0000", inv)} < β {last.Beta.Legitimacy.ToString("0.0000", inv)}"));

        var gammaGained = last.Gamma.Population > first.Gamma.Population;
        checks.Add(new CouplingCheck(
            gammaGained || model.Telemetry.PopulationMigrated > 0,
            "γ haven / spatial move",
            $"γ pop {first.Gamma.Population.ToString("0", inv)}→{last.Gamma.Population.ToString("0", inv)}; " +
            $"telemetry migrated {model.Telemetry.PopulationMigrated.ToString("0", inv)}"));

        var taxGapHeld =
            Math.Abs(last.Alpha.HouseholdTaxRate - model.Spec.AlphaTax) < 1e-6 &&
            Math.Abs(last.Beta.HouseholdTaxRate - model.Spec.BetaTax) < 1e-6;
        checks.Add(new CouplingCheck(
            taxGapHeld && model.Spec.AlphaTax > model.Spec.BetaTax + 0.05,
            "treatment locked",
            $"α tax {last.Alpha.HouseholdTaxRate.ToString("0.00", inv)} / " +
            $"β {last.Beta.HouseholdTaxRate.ToString("0.00", inv)} (spec gap held)"));

        return checks;
    }
}

sealed class PolityFacts
{
    public double Population { get; init; }
    public double NetMigration { get; init; }
    public double EmigrationPressure { get; init; }
    public double Legitimacy { get; init; }
    public double Approval { get; init; }
    public double HouseholdTaxRate { get; init; }
    public double ProductionValue { get; init; }
    public double ControlRatio { get; init; }
    public double TaxCollected { get; init; }
    public double StateCash { get; init; }
    public double OreStock { get; init; }
}

sealed class MonthSample
{
    public required int Month { get; init; }
    public required string Phase { get; init; }
    public required bool AtWar { get; init; }
    public required double TradeDelta { get; init; }
    public required PolityFacts Alpha { get; init; }
    public required PolityFacts Beta { get; init; }
    public required PolityFacts Gamma { get; init; }
}

readonly record struct CouplingCheck(bool Pass, string Claim, string Detail);

sealed class ExperimentResult
{
    public required ExperimentSpec Spec { get; init; }
    public required int SampleCount { get; init; }
    public required double AlphaPopStart { get; init; }
    public required double AlphaPopEnd { get; init; }
    public required double BetaPopStart { get; init; }
    public required double BetaPopEnd { get; init; }
    public required double GammaPopStart { get; init; }
    public required double GammaPopEnd { get; init; }
    public required double AlphaNetMigrationSum { get; init; }
    public required double AlphaPeakPressure { get; init; }
    public required double BetaPeakPressure { get; init; }
    public required double AlphaLegitimacyEnd { get; init; }
    public required double BetaLegitimacyEnd { get; init; }
    public required double PopulationMigrated { get; init; }
    public required IReadOnlyList<CouplingCheck> Checks { get; init; }

    public int PassCount => Checks.Count(c => c.Pass);
    public int CheckCount => Checks.Count;
    public bool AllPass => Checks.Count > 0 && Checks.All(c => c.Pass);

    public void WriteToConsole(TimeSpan elapsed)
    {
        var inv = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.AppendLine("=== MobilityLab — tax–mobility evidence ===");
        sb.AppendLine(
            $"Run: {SampleCount} months in {elapsed.TotalSeconds.ToString("0.00", inv)}s  " +
            $"α tax={Spec.AlphaTax.ToString("0.00", inv)} β={Spec.BetaTax.ToString("0.00", inv)} " +
            $"γ={Spec.GammaTax.ToString("0.00", inv)} warShock={Spec.WarShockOn} seed={Spec.Seed}");
        sb.AppendLine();
        sb.AppendLine("--- End stocks ---");
        sb.AppendLine(
            $"  α pop {AlphaPopStart.ToString("0", inv)} → {AlphaPopEnd.ToString("0", inv)}  " +
            $"netΣ {AlphaNetMigrationSum.ToString("0", inv)}  peak push {AlphaPeakPressure.ToString("0.00", inv)}  " +
            $"L {AlphaLegitimacyEnd.ToString("0.00", inv)}");
        sb.AppendLine(
            $"  β pop {BetaPopStart.ToString("0", inv)} → {BetaPopEnd.ToString("0", inv)}  " +
            $"peak push {BetaPeakPressure.ToString("0.00", inv)}  L {BetaLegitimacyEnd.ToString("0.00", inv)}");
        sb.AppendLine(
            $"  γ pop {GammaPopStart.ToString("0", inv)} → {GammaPopEnd.ToString("0", inv)}  " +
            $"telemetry migrated {PopulationMigrated.ToString("0", inv)}");
        sb.AppendLine();
        sb.AppendLine("--- Coupling checks ---");
        foreach (var c in Checks)
            sb.AppendLine($"  {(c.Pass ? "PASS" : "FAIL")}  [{c.Claim}] {c.Detail}");
        sb.AppendLine($"  Result: {PassCount}/{CheckCount} checks passed");
        Console.Write(sb.ToString());
    }
}

sealed class ExperimentHost
{
    public required TaxMobilityWorld.Model Model { get; init; }
    public required Queue<string> Log { get; init; }
    public required ExperimentResult Result { get; init; }

    public static ExperimentHost Run(ExperimentSpec spec)
    {
        var model = TaxMobilityWorld.Create(spec);
        var log = new Queue<string>();
        log.Enqueue(
            $"Experiment start α tax={spec.AlphaTax:0.00} β={spec.BetaTax:0.00} γ={spec.GammaTax:0.00} " +
            $"months={spec.Months} warShock={spec.WarShockOn}");

        for (var i = 0; i < spec.Months; i++)
        {
            TaxMobilityMonth.MaybeApplyWarShock(model, log, i);
            TaxMobilityMonth.Advance(model, log);
        }

        return new ExperimentHost
        {
            Model = model,
            Log = log,
            Result = model.History.Evaluate(model),
        };
    }

    public static TaxMobilityWorld.Model CreateFresh(ExperimentSpec spec) =>
        TaxMobilityWorld.Create(spec);
}
