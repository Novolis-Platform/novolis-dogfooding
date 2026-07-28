using Novolis.Economy;
using Novolis.Economy.Accounting;
using Novolis.Economy.Logistics;
using Novolis.Economy.Simulation;
using Spectre.Console;

namespace NearSolPolity;

/// <summary>Parses duration args like <c>100d</c>, <c>2000h</c>, or bare hours.</summary>
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

/// <summary>Plain-text end-of-run report for headless / CI runs.</summary>
internal static class HeadlessReport
{
  public static void Write(
    EconomySimulation sim,
    PolityWorld.Ids ids,
    CreditCirculation credits,
    decimal openingLiquid,
    long requestedHours,
    TimeSpan wall)
  {
    var world = sim.State.World;
    var tramp = world.Ledgers[ids.Tramp];
    var polity = world.Ledgers[ids.Polity];
    var hh = world.Cohorts.Sum(c => c.BudgetRemaining.Amount);
    var produced = sim.State.Events.OfType<BatchProduced>().Sum(e => e.Quantity.Value);
    var sold = sim.State.Events.OfType<GoodsSold>().Sum(e => e.Quantity.Value);
    var b2b = sim.State.Events.OfType<GoodsSoldInterFirm>().Sum(e => e.Quantity.Value);
    var delivered = world.TransportStats.CargoDelivered.Value;
    var day = sim.State.Clock.Date.DayIndex;
    var liquidDelta = credits.LiquidStock - openingLiquid;

    var lines = new[]
    {
      "=== Near-Sol Polity report ===",
      $"Sim time:     day {day} ({sim.State.Clock.HourIndex}h)  requested {DurationArg.Format(requestedHours)}",
      $"Wall clock:   {wall.TotalSeconds:0.#}s",
      $"Hash:         {sim.State.Hash:X16}",
      "",
      "— Money stock —",
      $"Liquid:       {credits.LiquidStock:0}  (open {openingLiquid:0}, Δ {liquidDelta:0})",
      $"  Polity:     {polity.Cash.Amount:0}",
      $"  Tramp:      {tramp.Cash.Amount:0}",
      $"  Households: {hh:0}",
      $"Wages→hh:     {credits.WagesDistributed:0}",
      $"Imports:      {credits.ImportSpend:0}",
      $"Tolls→polity: {credits.TollsToPolity:0.##}",
      "",
      "— Activity —",
      $"Produced:     {produced:0}",
      $"Retail sold:  {sold:0}",
      $"B2B qty:      {b2b:0}",
      $"Delivered:    {delivered:0}",
      $"Fuel burned:  {world.TransportStats.FuelBurned.Value:0.#}",
      $"Plan fails:   {world.TransportStats.FailedPlans}",
      "",
      "— Tramp opex (ledger) —",
      $"Fuel expense: {tramp.Balance(AccountRole.TransportFuelExpense).Amount:0.##}",
      $"Toll expense: {tramp.Balance(AccountRole.TransportTollExpense).Amount:0.##}",
      $"Wage expense: {tramp.Balance(AccountRole.WageExpense).Amount:0.##}",
      $"Revenue:      {Math.Abs(tramp.Balance(AccountRole.Revenue).Amount):0}",
      "",
      "— Travel model —",
      $"Cruise:       {AstroEconomyBridge.CruiseDaysPerLy:0.##} day(s)/ly  ({AstroEconomyBridge.CruiseLyPerHour:0.####} ly/h)",
      $"Example Sol→α Cen (~4.4 ly): {AstroEconomyBridge.TransitHours(4.4)}h / {AstroEconomyBridge.TransitDays(4.4):0.#}d",
      "===============================",
    };

    foreach (var line in lines)
    {
      AnsiConsole.WriteLine(line);
    }
  }
}
