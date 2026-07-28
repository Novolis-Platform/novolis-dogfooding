using Novolis.Economy;
using Novolis.Economy.Accounting;
using Novolis.Economy.Finance;
using Novolis.Economy.Markets;
using Novolis.Economy.Population;
using Novolis.Economy.Production;
using Novolis.Economy.Simulation;
using Spectre.Console;

namespace NearSolPolity;

internal static class DurationArg
{
  public static bool TryParse(string? text, out long hours)
  {
    hours = 0;
    if (string.IsNullOrWhiteSpace(text))
    {
      return false;
    }

    text = text.Trim();
    var suffix = text[^1];
    var numberPart = char.IsLetter(suffix) ? text[..^1] : text;
    if (!decimal.TryParse(numberPart, System.Globalization.NumberStyles.Number,
          System.Globalization.CultureInfo.InvariantCulture, out var value)
        || value <= 0m)
    {
      return false;
    }

    hours = suffix switch
    {
      'd' or 'D' => (long)Math.Ceiling(value * SimulationHour.HoursPerDay),
      'h' or 'H' => (long)Math.Ceiling(value),
      _ when !char.IsLetter(suffix) => (long)Math.Ceiling(value),
      _ => 0,
    };
    return hours > 0;
  }

  public static string Format(long hours) =>
    hours % SimulationHour.HoursPerDay == 0
      ? $"{hours / SimulationHour.HoursPerDay}d ({hours}h)"
      : $"{hours}h (~{hours / (double)SimulationHour.HoursPerDay:0.#}d)";
}

internal static class HeadlessReport
{
  public static void Write(
    EconomySimulation sim,
    PolityWorld.Ids ids,
    CreditCirculation credits,
    decimal openingLiquid,
    long requestedHours,
    TimeSpan wall,
    NearSolAgents.Bundle agents)
  {
    var world = sim.State.World;
    var hh = world.Cohorts.Sum(c => c.BudgetRemaining.Amount);
    var delivered = world.TransportStats.CargoDelivered.Value;
    var day = sim.State.Clock.Date.DayIndex;
    var liquidDelta = credits.LiquidStock - openingLiquid;
    var openBuy = world.HubOrders.Count(o => !o.IsFilled && o.Side == HubOrderSide.Buy);
    var openSell = world.HubOrders.Count(o => !o.IsFilled && o.Side == HubOrderSide.Sell);

    var lines = new List<string>
    {
      "=== Near-Sol Tycoon report ===",
      $"Sim time:     day {day} ({sim.State.Clock.HourIndex}h)  requested {DurationArg.Format(requestedHours)}",
      $"Wall clock:   {wall.TotalSeconds:0.#}s",
      $"Hash:         {sim.State.Hash:X16}",
      "",
      "— Money stock —",
      $"Liquid:       {credits.LiquidStock:0}  (open {openingLiquid:0}, Δ {liquidDelta:0})",
      $"  vs imports: Δ+imports = {liquidDelta + credits.ImportSpend:0} (want ~0)",
      $"  Households: {hh:0}",
      $"Inv book $:   {credits.InventoryBookValue:0}",
      $"Wages→hh:     {credits.WagesDistributed:0}",
      $"Imports:      {credits.ImportSpend:0}",
      $"Tolls→Civics: {credits.TollsToTreasury:0.##}",
      "",
      "— Finance —",
      $"Loans:        active {credits.ActiveLoans}  originated {credits.LoansOriginated}  defaults {credits.LoansDefaulted}",
      $"Principal:    {credits.PrincipalOutstanding:0}",
      $"Interest:     accrued {credits.InterestAccrued:0.##}  repaid-cash {credits.InterestPaid:0.##}",
      "",
      "— Entities (kind / registry / frozen) —",
    };

    foreach (var (name, firmId) in ids.Firms)
    {
      var entity = world.Entities.GetValueOrDefault(firmId);
      var kind = entity?.Kind.ToString() ?? "?";
      var reg = entity?.RegistryId ?? "—";
      var frozen = entity?.CreditFrozen == true ? "frozen" : "ok";
      lines.Add($"  {name,-9} {kind,-5}  registry {reg}  credit {frozen}");
    }

    lines.Add("");
    lines.Add("— Ownership —");
    if (world.OwnershipClaims.Count == 0)
    {
      lines.Add("  (none)");
    }
    else
    {
      foreach (var claim in world.OwnershipClaims
                 .OrderBy(c => c.IssuerFirmId.Value)
                 .ThenBy(c => c.OwnerFirmId.Value))
      {
        var issuer = FirmName(ids, claim.IssuerFirmId);
        var owner = FirmName(ids, claim.OwnerFirmId);
        lines.Add($"  {owner} owns {claim.Fraction:0.##} of {issuer}");
      }
    }

    lines.Add("");
    lines.Add("— Firms (cash / rev / COGS / wages / interest / notes) —");

    foreach (var (name, firmId) in ids.Firms)
    {
      var ledger = world.Ledgers[firmId];
      lines.Add(
        $"  {name,-9} cash {ledger.Cash.Amount,7:0}  rev {Abs(ledger, AccountRole.Revenue),7:0}  " +
        $"cogs {Abs(ledger, AccountRole.CostOfGoodsSold),6:0}  wage {Abs(ledger, AccountRole.WageExpense),6:0}  " +
        $"intE {Abs(ledger, AccountRole.InterestExpense),5:0}  intI {Abs(ledger, AccountRole.InterestIncome),5:0}  " +
        $"NP {Abs(ledger, AccountRole.NotesPayable),5:0}  NR {Abs(ledger, AccountRole.NotesReceivable),5:0}");
    }

    lines.Add("");
    lines.Add("— Macro events (counts) —");
    lines.Add(
      $"Credit:      originated {credits.LoansOriginated}  defaults {credits.LoansDefaulted}  " +
      $"freezes {credits.CreditFreezes} (now frozen {credits.CreditFrozenFirms})");
    lines.Add(
      $"Ownership:   changes {credits.OwnershipChanges}  dividends {credits.Dividends} " +
      $"(cash {credits.DividendCash:0})  absorbs {credits.FacilitiesAbsorbed}");
    lines.Add(
      $"Capacity:    upgrades {credits.FacilityUpgrades}  upgrade-fails {credits.FacilityUpgradeFails}");
    lines.Add(
      $"B2B xfer:    fills {credits.B2bFills} (qty {credits.B2bQty:0})  " +
      $"fail cash {credits.B2bFailCash} / stock {credits.B2bFailStock}");

    lines.Add("");
    lines.Add("— Summary ratios —");
    var firmCash = ids.Firms.Sum(f => world.Ledgers[f.Id].Cash.Amount);
    var totalCashPool = firmCash + hh;
    var days = Math.Max(1, day);
    lines.Add(
      $"Cash split:  firms {firmCash:0} ({Pct(firmCash, totalCashPool)})  " +
      $"hh {hh:0} ({Pct(hh, totalCashPool)})  inv-book {credits.InventoryBookValue:0}");
    lines.Add($"Per day:     fills {credits.BookFills / (decimal)days:0.##}  " +
              $"delivered {delivered / days:0.##}  produced {credits.Produced / days:0.##}  " +
              $"retail {credits.RetailSold / days:0.##}");
    lines.Add(
      $"Throughput:  retail/produced {Ratio(credits.RetailSold, credits.Produced)}  " +
      $"delivered/departed {Ratio(delivered, credits.Departed)}  " +
      $"fill qty/fill {Ratio(credits.BookFillQty, credits.BookFills)}");
    lines.Add("Firm cash % of firm pool:");
    if (firmCash > 0m)
    {
      foreach (var (name, firmId) in ids.Firms)
      {
        var c = world.Ledgers[firmId].Cash.Amount;
        lines.Add($"  {name,-9} {c,7:0}  {Pct(c, firmCash)}");
      }
    }

    if (credits.Milestones.Count > 0)
    {
      lines.Add("");
      lines.Add("— Milestones (cumulative) —");
      lines.Add(
        "  day   liquid    hh   inv$  prod  retail fills  deliv  defs  div$  abs  upg");
      MacroSnapshot? prev = null;
      foreach (var m in credits.Milestones)
      {
        lines.Add(
          $"  {m.DayIndex,4}  {m.Liquid,7:0}  {m.Households,5:0}  {m.InventoryBook,5:0}  " +
          $"{m.Produced,5:0}  {m.RetailSold,5:0}  {m.BookFills,5}  {m.Delivered,5:0}  " +
          $"{m.LoansDefaulted,4}  {m.DividendsPaid,4:0}  {m.FacilitiesAbsorbed,3}  {m.Upgrades,3}");
        if (prev is { } p)
        {
          var dDays = Math.Max(1, m.DayIndex - p.DayIndex);
          lines.Add(
            $"       Δ/day fills {(m.BookFills - p.BookFills) / (decimal)dDays:0.##}  " +
            $"deliv {(m.Delivered - p.Delivered) / dDays:0.##}  " +
            $"hh Δ {m.Households - p.Households:0}");
        }

        prev = m;
      }
    }

    if (credits.MacroLog.Count > 0)
    {
      lines.Add("");
      lines.Add("— Macro event log (recent) —");
      foreach (var line in credits.MacroLog)
      {
        lines.Add($"  {line}");
      }
    }

    lines.Add("");
    lines.Add("— Habitats / regions —");
    foreach (var region in world.Regions.Values.OrderBy(r => r.AreaId.Value))
    {
      var usedLive = world.UsedLivingHouseholds(region.AreaId);
      var usedProd = world.UsedProductionSlots(region.AreaId);
      var pool = world.Cohorts
        .Where(c => c.Definition.Area.Equals(region.AreaId))
        .Sum(c => HouseholdMath.LaborHoursPerTick(
          c.Definition.Population,
          c.Definition.Productivity,
          world.Policy.PeoplePerHousehold));
      lines.Add(
        $"  {region.AreaId.Value.ToString("N")[..8]}…  living {usedLive}/{region.LivingCapacityHouseholds}  " +
        $"mfg-slots {usedProd}/{region.ProductionSlots}  pool-h/tick {pool:0.##}");
    }

    lines.Add("");
    lines.Add("— Household cohorts (budget / comfort / productivity) —");
    var comfortHolds = agents.Households.Count(a => a.LastDecision == "comfort hold");
    lines.Add($"  Agents: {agents.Households.Count}  comfort holds (last pulse): {comfortHolds}");
    foreach (var cohort in world.Cohorts.OrderByDescending(c => c.BudgetRemaining.Amount).Take(8))
    {
      var floor = world.ComfortFloor(cohort).Amount;
      var above = world.IsAboveComfort(cohort) ? "above" : "hold";
      var prod = cohort.Definition.Productivity.ToString();
      lines.Add(
        $"  bud {cohort.BudgetRemaining.Amount,7:0}  floor {floor,5:0}  {above,-5}  {prod,-7}  " +
        $"pop {cohort.Definition.Population.Value}");
    }

    lines.Add("");
    lines.Add("— Facility owners —");
    foreach (var fac in world.Facilities.Values.OrderBy(f => f.Id.Value).Take(8))
    {
      lines.Add($"  {fac.Id.Value.ToString("N")[..8]}… → {FirmName(ids, fac.FirmId)}");
    }

    var depth = world.HubOrders.Where(o => !o.IsFilled)
      .GroupBy(o => PolityWorld.SkuLabel(o.ProductId, ids))
      .OrderByDescending(g => g.Count())
      .Take(4)
      .Select(g =>
      {
        var buy = g.Where(o => o.Side == HubOrderSide.Buy).Sum(o => o.Remaining.Value);
        var sell = g.Where(o => o.Side == HubOrderSide.Sell).Sum(o => o.Remaining.Value);
        return $"{g.Key} b{buy:0}/s{sell:0}";
      });

    lines.AddRange(
    [
      "",
      "— Activity —",
      $"Produced:     {credits.Produced:0}",
      $"Retail sold:  {credits.RetailSold:0}",
      $"Book fills:   {credits.BookFills}  (qty {credits.BookFillQty:0})  open buy {openBuy} / sell {openSell}",
      $"Book depth:   {string.Join(" · ", depth)}",
      $"Delivered:    {delivered:0}",
      $"Departed:     {credits.Departed}",
      $"Fuel burned:  {world.TransportStats.FuelBurned.Value:0.#}",
      $"Plan fails:   {world.TransportStats.FailedPlans}",
      "",
      "— Agents —",
      $"Mining:       {agents.Mining.LastDecision}",
      $"Industry:     {agents.Industry.LastDecision}",
      $"Station:      {agents.Station.LastDecision}",
    ]);
    for (var i = 0; i < agents.Carriers.Count; i++)
    {
      var c = agents.Carriers[i];
      var label = i == 0 ? "Carrier" : $"Tramp{i + 1}";
      lines.Add($"{label + ":",-14}{c.LastDecision} | {c.LastEval}");
    }

    lines.AddRange(
    [
      $"Treasury:     {agents.Treasury.LastDecision}",
      "",
      "— Travel —",
      $"Cruise:       {AstroEconomyBridge.CruiseDaysPerLy:0.##} d/ly",
      $"Fleet:        {agents.Carriers.Count} tramps  MinMargin {PolityWorld.MinMargin:0}",
      $"Roles:        {ids.RoleSummary}",
      "Agents:       Novolis.Economy.Agents heuristics + DeterministicRandom",
      "Civics:       Station entity (tolls / treasury / ownership) — product copy",
      "Geography:    SystemProfile potentials (Astro.Assessment) gate settlement/mining",
    ]);

    var failReasons = credits.PlanFailReasons
      .OrderByDescending(kv => kv.Value)
      .Take(5)
      .Select(kv => $"{kv.Key}×{kv.Value}");
    if (failReasons.Any())
    {
      lines.Add($"Fail reasons: {string.Join(", ", failReasons)}");
    }

    lines.Add("===============================");

    foreach (var line in lines)
    {
      AnsiConsole.WriteLine(line);
    }
  }

  private static string FirmName(PolityWorld.Ids ids, FirmId id)
  {
    foreach (var (name, firmId) in ids.Firms)
    {
      if (firmId.Equals(id))
      {
        return name;
      }
    }

    return id.Value.ToString("N")[..8];
  }

  private static decimal Abs(FirmLedger ledger, AccountRole role) =>
    Math.Abs(ledger.Balance(role).Amount);

  private static string Pct(decimal part, decimal whole) =>
    whole <= 0m ? "—" : $"{100m * part / whole:0.#}%";

  private static string Ratio(decimal num, decimal den) =>
    den <= 0m ? "—" : $"{num / den:0.##}";
}
