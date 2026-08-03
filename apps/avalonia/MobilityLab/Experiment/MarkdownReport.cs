using System.Globalization;
using System.Text;

namespace MobilityLab.Experiment;

static class MarkdownReport
{
    public static string Build(ExperimentResult result, TaxMobilityWorld.Model model, TimeSpan? elapsed = null)
    {
        var inv = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        var months = model.History.Months;
        var e = result.Effects;
        var id = result.Identification;

        sb.AppendLine("# MobilityLab evidence report");
        sb.AppendLine();
        sb.AppendLine("## Spec");
        sb.AppendLine();
        sb.AppendLine("| Parameter | Value |");
        sb.AppendLine("|-----------|-------|");
        sb.AppendLine($"| Alpha tax (treatment) | {result.Spec.AlphaTax.ToString("0.00", inv)} |");
        sb.AppendLine($"| Beta tax (control) | {result.Spec.BetaTax.ToString("0.00", inv)} |");
        sb.AppendLine($"| Gamma tax (haven) | {result.Spec.GammaTax.ToString("0.00", inv)} |");
        sb.AppendLine($"| Months | {result.SampleCount} / {result.Spec.Months} |");
        sb.AppendLine($"| Seed | {result.Spec.Seed} |");
        sb.AppendLine($"| War shock | {result.Spec.WarShockOn} |");
        sb.AppendLine($"| Agents | {result.Spec.AgentsEnabled} |");
        if (elapsed is { } t)
            sb.AppendLine($"| Elapsed (treated+CF) | {t.TotalSeconds.ToString("0.00", inv)}s |");
        sb.AppendLine();

        sb.AppendLine("## Research question");
        sb.AppendLine();
        sb.AppendLine(
            "Holding geography and initial stocks fixed, does raising polity Alpha's household tax " +
            "above Economy tax-push / Civics emigration thresholds cause (1) net population outflow, " +
            "(2) higher emigration pressure, and (3) weaker early approval / civic strain vs low-tax twin Beta, " +
            "with Gamma as a low-tax destination?");
        sb.AppendLine();
        sb.AppendLine(
            "**Estimator:** same-seed **counterfactual** where Alpha tax = Beta tax (no treatment gap), " +
            "plus within-world twin **DID** on population growth. End-horizon legitimacy is reported as a " +
            "diagnostic only (often ceiling-bound).");
        sb.AppendLine();

        sb.AppendLine("## Identification");
        sb.AppendLine();
        sb.AppendLine("| Check | Value |");
        sb.AppendLine("|-------|-------|");
        sb.AppendLine($"| Counterfactual valid (Alpha tax=Beta tax, same seed/horizon, war off) | {id.CounterfactualValid} |");
        sb.AppendLine($"| Treatment tax gap | {id.TreatmentTaxGap.ToString("0.00", inv)} |");
        sb.AppendLine($"| Tax locked at horizon | {id.TaxLockedAtHorizon} |");
        sb.AppendLine($"| War shock | {id.WarShockOn} |");
        sb.AppendLine($"| Agents | {id.AgentsEnabled} |");
        sb.AppendLine($"| Twin balance gap at M1 | {id.TwinBalanceGapM1.ToString("0.0%", inv)} |");
        sb.AppendLine($"| Burn-in (excluded from mean push) | M1-M{id.BurnInMonths} |");
        sb.AppendLine($"| Early civic window | M1-M{id.EarlyCivicWindow} |");
        sb.AppendLine($"| Post-burn-in months | {id.PostSampleMonths} |");
        sb.AppendLine();

        sb.AppendLine("## Effect sizes");
        sb.AppendLine();
        sb.AppendLine("| Estimand | Value | Notes |");
        sb.AppendLine("|----------|-------|-------|");
        sb.AppendLine(
            $"| ATT Alpha population | {e.AttAlphaPop.ToString("+0;-0;0", inv)} " +
            $"({e.AttAlphaPopPct.ToString("+0.0%;-0.0%;0%", inv)}) | " +
            $"treated {e.TreatedAlphaPopEnd.ToString("0", inv)} vs CF {e.CounterfactualAlphaPopEnd.ToString("0", inv)} |");
        sb.AppendLine(
            $"| ATT Alpha net migration sum | {e.AttAlphaNetMigration.ToString("+0;-0;0", inv)} | treated minus CF |");
        sb.AppendLine(
            $"| ATT mean emigration pressure | {e.AttMeanPush.ToString("+0.000;-0.000;0", inv)} | post burn-in |");
        sb.AppendLine(
            $"| Twin DID pop growth (Alpha-Beta) | {e.DidPopGrowth.ToString("+0.0%;-0.0%;0%", inv)} | within treated world |");
        sb.AppendLine(
            $"| Mean push gap Alpha-Beta | {e.MeanPushGapVsBeta.ToString("+0.000;-0.000;0", inv)} | post burn-in |");
        sb.AppendLine(
            $"| Push dominance share | {e.PushDominanceShare.ToString("0%", inv)} | fraction of post months Alpha>Beta |");
        sb.AppendLine(
            $"| Gamma absorb share | {e.GammaAbsorbShare.ToString("0.0%", inv)} | Gamma gain / Alpha loss |");
        sb.AppendLine(
            $"| Early approval gap Alpha-Beta | {e.EarlyApprovalGap.ToString("+0.000;-0.000;0", inv)} | M1-M{id.EarlyCivicWindow} means |");
        sb.AppendLine(
            $"| ATT early approval | {e.AttEarlyApproval.ToString("+0.000;-0.000;0", inv)} | vs CF Alpha |");
        sb.AppendLine(
            $"| Early legitimacy gap (diagnostic) | {e.EarlyLegitimacyGap.ToString("+0.000;-0.000;0", inv)} | often weak if L ceilings |");
        sb.AppendLine();

        sb.AppendLine("## End stocks (treated)");
        sb.AppendLine();
        sb.AppendLine("| Polity | Pop start | Pop end | Delta | Peak push | L end |");
        sb.AppendLine("|--------|-----------|---------|-------|-----------|-------|");
        sb.AppendLine(
            $"| Alpha | {result.AlphaPopStart.ToString("0", inv)} | {result.AlphaPopEnd.ToString("0", inv)} | " +
            $"{(result.AlphaPopEnd - result.AlphaPopStart).ToString("+0;-0;0", inv)} | " +
            $"{result.AlphaPeakPressure.ToString("0.00", inv)} | {result.AlphaLegitimacyEnd.ToString("0.0000", inv)} |");
        sb.AppendLine(
            $"| Beta | {result.BetaPopStart.ToString("0", inv)} | {result.BetaPopEnd.ToString("0", inv)} | " +
            $"{(result.BetaPopEnd - result.BetaPopStart).ToString("+0;-0;0", inv)} | " +
            $"{result.BetaPeakPressure.ToString("0.00", inv)} | {result.BetaLegitimacyEnd.ToString("0.0000", inv)} |");
        sb.AppendLine(
            $"| Gamma | {result.GammaPopStart.ToString("0", inv)} | {result.GammaPopEnd.ToString("0", inv)} | " +
            $"{(result.GammaPopEnd - result.GammaPopStart).ToString("+0;-0;0", inv)} | - | - |");
        sb.AppendLine();
        sb.AppendLine(
            $"- Alpha net migration sum (treated): **{result.AlphaNetMigrationSum.ToString("0", inv)}**");
        sb.AppendLine(
            $"- Telemetry population migrated (treated): **{result.PopulationMigrated.ToString("0", inv)}**");
        sb.AppendLine();

        sb.AppendLine("## Coupling checks");
        sb.AppendLine();
        sb.AppendLine($"**Result: {result.PassCount}/{result.CheckCount} " +
                      $"{(result.AllPass ? "PASS" : "MIXED")}**");
        sb.AppendLine();
        sb.AppendLine("| Status | Claim | Detail |");
        sb.AppendLine("|--------|-------|--------|");
        foreach (var c in result.Checks)
            sb.AppendLine($"| {(c.Pass ? "PASS" : "FAIL")} | {Ascii(c.Claim)} | {Ascii(c.Detail)} |");
        sb.AppendLine();

        sb.AppendLine("## Interpretation guide");
        sb.AppendLine();
        sb.AppendLine(
            "- **ATT Alpha pop < 0** is the primary causal claim (high tax vs same-seed Alpha at control tax).");
        sb.AppendLine(
            "- **DID Alpha vs Beta growth** is the twin contrast; both can move, but Alpha should fall relative to Beta.");
        sb.AppendLine(
            "- **Pressure** uses post-burn-in means so M1 zeros do not dilute the signal.");
        sb.AppendLine(
            "- **Early approval** is the civic tax channel; do not over-read end L when both sit near 1.0.");
        sb.AppendLine(
            "- With **war shock on**, treat ATT as confounded; identification check should FAIL.");
        sb.AppendLine();

        sb.AppendLine("## Province map (end, treated)");
        sb.AppendLine();
        sb.AppendLine("| Province | Owner | Home | Population |");
        sb.AppendLine("|----------|-------|------|------------|");
        foreach (var p in model.World.Provinces.OrderBy(x => x.Id.Value))
        {
            sb.AppendLine(
                $"| {p.Name} | {Tag(p.OwnerId)} | {Tag(p.HomePolityId)} | " +
                $"{p.Population.ToString("0", inv)} |");
        }

        sb.AppendLine();
        sb.AppendLine("## Series snapshot (every 6th month, treated)");
        sb.AppendLine();
        sb.AppendLine("| M | Alpha pop | Beta pop | Gamma pop | Alpha push | Beta push | Alpha App | Beta App | Alpha L | Beta L |");
        sb.AppendLine("|---|-----------|----------|-----------|------------|-----------|-----------|----------|---------|--------|");
        for (var i = 0; i < months.Count; i++)
        {
            if (i != 0 && (i + 1) % 6 != 0 && i != months.Count - 1)
                continue;
            var m = months[i];
            sb.AppendLine(
                $"| {m.Month} | {m.Alpha.Population.ToString("0", inv)} | " +
                $"{m.Beta.Population.ToString("0", inv)} | {m.Gamma.Population.ToString("0", inv)} | " +
                $"{m.Alpha.EmigrationPressure.ToString("0.00", inv)} | " +
                $"{m.Beta.EmigrationPressure.ToString("0.00", inv)} | " +
                $"{m.Alpha.Approval.ToString("0.00", inv)} | " +
                $"{m.Beta.Approval.ToString("0.00", inv)} | " +
                $"{m.Alpha.Legitimacy.ToString("0.00", inv)} | " +
                $"{m.Beta.Legitimacy.ToString("0.00", inv)} |");
        }

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine("*Generated by MobilityLab - paste this block into chat for review.*");
        return sb.ToString();

        static string Tag(Novolis.Geopolitics.Core.PolityId id) =>
            id.Value switch { 0 => "Alpha", 1 => "Beta", _ => "Gamma" };

        static string Ascii(string text) =>
            text.Replace("α", "Alpha", StringComparison.Ordinal)
                .Replace("β", "Beta", StringComparison.Ordinal)
                .Replace("γ", "Gamma", StringComparison.Ordinal)
                .Replace("→", "->", StringComparison.Ordinal)
                .Replace("Σ", "sum", StringComparison.Ordinal)
                .Replace("≥", ">=", StringComparison.Ordinal)
                .Replace("–", "-", StringComparison.Ordinal);
    }
}
