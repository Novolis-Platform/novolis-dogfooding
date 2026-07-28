using Novolis.Economy;
using Novolis.Economy.Accounting;
using Novolis.Economy.Finance;
using Novolis.Economy.Markets;
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
      $"Tolls→Station:{credits.TollsToTreasury:0.##}",
      "",
      "— Finance —",
      $"Loans:        active {credits.ActiveLoans}  originated {credits.LoansOriginated}  defaults {credits.LoansDefaulted}",
      $"Principal:    {credits.PrincipalOutstanding:0}",
      $"Interest:     accrued {credits.InterestAccrued:0.##}  repaid-cash {credits.InterestPaid:0.##}",
      "",
      "— Firms (cash / rev / COGS / wages / interest / notes) —",
    };

    foreach (var (name, firmId) in ids.Firms)
    {
      var ledger = world.Ledgers[firmId];
      lines.Add(
        $"  {name,-9} cash {ledger.Cash.Amount,7:0}  rev {Abs(ledger, AccountRole.Revenue),7:0}  " +
        $"cogs {Abs(ledger, AccountRole.CostOfGoodsSold),6:0}  wage {Abs(ledger, AccountRole.WageExpense),6:0}  " +
        $"intE {Abs(ledger, AccountRole.InterestExpense),5:0}  intI {Abs(ledger, AccountRole.InterestIncome),5:0}  " +
        $"NP {Abs(ledger, AccountRole.NotesPayable),5:0}  NR {Abs(ledger, AccountRole.NotesReceivable),5:0}");
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
      $"Carrier:      {agents.Carrier.LastDecision} | {agents.Carrier.LastEval}",
      $"Treasury:     {agents.Treasury.LastDecision}",
      "",
      "— Travel —",
      $"Cruise:       {AstroEconomyBridge.CruiseDaysPerLy:0.##} d/ly",
      "Agents:       Novolis.Economy.Agents heuristics + DeterministicRandom",
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

  private static decimal Abs(FirmLedger ledger, AccountRole role) =>
    Math.Abs(ledger.Balance(role).Amount);
}
