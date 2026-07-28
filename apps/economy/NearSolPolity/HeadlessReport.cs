using Novolis.Economy;
using Novolis.Economy.Accounting;
using Novolis.Economy.Markets;
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
    CarrierHeuristic? carrier = null)
  {
    var world = sim.State.World;
    var hh = world.Cohorts.Sum(c => c.BudgetRemaining.Amount);
    var delivered = world.TransportStats.CargoDelivered.Value;
    var day = sim.State.Clock.Date.DayIndex;
    var liquidDelta = credits.LiquidStock - openingLiquid;
    var openOrders = world.HubOrders.Count(o => !o.IsFilled);

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
      $"Wages→hh:     {credits.WagesDistributed:0}",
      $"Imports:      {credits.ImportSpend:0}",
      $"Tolls→Station:{credits.TollsToTreasury:0.##}",
      "",
      "— Firms (cash / revenue) —",
    };

    foreach (var (name, firmId) in ids.Firms)
    {
      var ledger = world.Ledgers[firmId];
      lines.Add(
        $"  {name,-9} cash {ledger.Cash.Amount,8:0}  rev {Math.Abs(ledger.Balance(AccountRole.Revenue).Amount),8:0}");
    }

    lines.AddRange(
    [
      "",
      "— Activity —",
      $"Produced:     {credits.Produced:0}",
      $"Retail sold:  {credits.RetailSold:0}",
      $"Book fills:   {credits.BookFills}  (qty {credits.BookFillQty:0})  open {openOrders}",
      $"Delivered:    {delivered:0}",
      $"Departed:     {credits.Departed}",
      $"Fuel burned:  {world.TransportStats.FuelBurned.Value:0.#}",
      $"Plan fails:   {world.TransportStats.FailedPlans}",
      "",
      "— Travel / freight —",
      $"Cruise:       {AstroEconomyBridge.CruiseDaysPerLy:0.##} d/ly",
      "Agents:       heuristics + DeterministicRandom jitter only",
    ]);
    if (carrier is not null)
    {
      lines.Add($"Carrier last: {carrier.LastDecision} | {carrier.LastEval}");
    }

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
}
