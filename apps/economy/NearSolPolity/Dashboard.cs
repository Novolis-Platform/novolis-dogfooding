using Novolis.Economy;
using Novolis.Economy.Accounting;
using Novolis.Economy.Logistics;
using Novolis.Economy.Production;
using Novolis.Economy.Simulation;
using Spectre.Console;

namespace NearSolPolity;

internal static class Dashboard
{
  public static Table Build(
    EconomySimulation sim,
    PolityWorld.Ids ids,
    PolityController polity,
    TrampFleetAutopilot tramp,
    CreditCirculation credits,
    IReadOnlyCollection<string> log,
    bool running,
    int hoursPerPulse,
    int pulseMs)
  {
    var world = sim.State.World;
    var trampLedger = world.Ledgers[ids.Tramp];
    var polityLedger = world.Ledgers[ids.Polity];
    var stats = world.TransportStats;
    var households = world.Cohorts.Sum(c => c.BudgetRemaining.Amount);
    var ship = world.Shipments.FirstOrDefault(s =>
      !s.IsLegacy && s.FirmId.Equals(ids.Tramp) && s.Status == ShipmentStatus.InTransit);

    var root = new Table().Border(TableBorder.Rounded).Expand();
    root.AddColumn(new TableColumn("[steelblue1]Near-Sol Polity[/]").Width(48));
    root.AddColumn(new TableColumn("[grey]Network[/]"));

    var left = new Table().HideHeaders().Border(TableBorder.None);
    left.AddColumn("k");
    left.AddColumn("v");
    left.AddRow("Hour", $"[bold]{sim.State.Clock.HourIndex}[/] (day {sim.State.Clock.Date.DayIndex})");
    left.AddRow("Speed", running
      ? $"[green]{hoursPerPulse}h[/] / {pulseMs}ms"
      : "[yellow]PAUSED[/]");
    left.AddRow("Systems", $"{ids.Bridge.Hubs.Count} hubs · {ids.Bridge.Corridors.Count / 2} lanes");
    left.AddRow("Roles", $"[aqua]{Markup.Escape(ids.RoleSummary)}[/]");
    left.AddRow("Liquid $", $"[bold]{credits.LiquidStock:0}[/] (firms+hh)");
    left.AddRow("Households", $"{households:0}");
    left.AddRow("Wages→hh", $"{credits.WagesDistributed:0}");
    left.AddRow("Imports", $"{credits.ImportSpend:0}");
    left.AddRow("Tramp cash", $"[green]{trampLedger.Cash.Amount:0}[/]");
    left.AddRow("Polity cash", $"{polityLedger.Cash.Amount:0}");
    left.AddRow("Fuel opex", $"{trampLedger.Balance(AccountRole.TransportFuelExpense).Amount:0.##}");
    left.AddRow("Tolls", $"{trampLedger.Balance(AccountRole.TransportTollExpense).Amount:0.##}");
    left.AddRow("Wages", $"{trampLedger.Balance(AccountRole.WageExpense).Amount:0.##}");
    left.AddRow("Tramp rev", $"{Math.Abs(trampLedger.Balance(AccountRole.Revenue).Amount):0}");
    left.AddRow("Delivered", $"{stats.CargoDelivered.Value:0}  burn {stats.FuelBurned.Value:0.#}");
    left.AddRow("Fails", $"{stats.FailedPlans}");
    left.AddRow("Polity", $"[grey]{Markup.Escape(polity.LastAction)}[/]");
    left.AddRow("Tramp", $"[yellow]{Markup.Escape(tramp.LastDecision)}[/]");
    left.AddRow("Eval", $"[grey]{Markup.Escape(Truncate(tramp.LastEval, 70))}[/]");
    left.AddRow("Highlights", Markup.Escape(StockHighlights(sim, ids)));

    if (ship is null)
    {
      var hubName = world.Hubs.TryGetValue(tramp.CurrentHub, out var h) ? h.Name : "?";
      left.AddRow("Hull", $"[grey]docked @ {Markup.Escape(hubName)}[/]");
    }
    else
    {
      var hubName = world.Hubs.TryGetValue(ship.CurrentHubId, out var h) ? h.Name : "?";
      left.AddRow("Hull", $"[aqua]{ship.Phase}[/] @ {Markup.Escape(hubName)}");
      left.AddRow("Leg", $"{ship.LegIndex}/{ship.Itinerary.LegCount}  seg {ship.SegmentHoursRemaining}h");
      left.AddRow("Fuel onboard", $"{ship.OnboardFuel.Value:0.##}");
    }

    var map = new Panel(BuildRouteStrip(ids, ship, tramp.CurrentHub))
    {
      Header = new PanelHeader(" Route strip "),
      Border = BoxBorder.Square,
    };
    var journal = new Panel(string.Join('\n', log.Reverse().Select(l => $"[grey]{Markup.Escape(l)}[/]")))
    {
      Header = new PanelHeader(" Log "),
      Border = BoxBorder.Rounded,
    };

    root.AddRow(left, new Rows(map, journal));
    return root;
  }

  private static string StockHighlights(EconomySimulation sim, PolityWorld.Ids ids)
  {
    var world = sim.State.World;
    var mines = ids.Sites.Values
      .Where(s => s.Hub.Role == SystemRole.Mining)
      .Select(s => (
        Name: Short(s.Hub.Name),
        Ore: world.Inventory.GetQuantity(new InventoryKey(ids.Polity, s.Hub.LocationId, ids.Ore)).Value))
      .OrderByDescending(x => x.Ore)
      .Take(2)
      .Select(x => $"M:{x.Name} raw{x.Ore:0}");

    var plants = ids.Sites.Values
      .Where(s => s.Hub.Role == SystemRole.Industrial)
      .Select(s => (
        Name: Short(s.Hub.Name),
        Parts: world.Inventory.GetQuantity(new InventoryKey(ids.Polity, s.Hub.LocationId, ids.Parts)).Value,
        Goods: world.Inventory.GetQuantity(new InventoryKey(ids.Polity, s.Hub.LocationId, ids.Goods)).Value))
      .OrderByDescending(x => x.Parts + x.Goods)
      .Take(2)
      .Select(x => $"I:{x.Name} cap{x.Parts:0}/fin{x.Goods:0}");

    return string.Join(" · ", mines.Concat(plants));
  }

  private static string BuildRouteStrip(
    PolityWorld.Ids ids,
    ActiveShipment? ship,
    TransportHubId current)
  {
    var focus = ship?.CurrentHubId ?? current;
    var hub = ids.Bridge.Hubs.FirstOrDefault(h => h.HubId.Equals(focus))
              ?? ids.Sites["sol"].Hub;

    var neighbors = ids.Bridge.Graph.Adjacency.TryGetValue(hub.SystemId, out var edges)
      ? edges.OrderBy(e => e.DistanceLy).Take(5).ToList()
      : [];

    var lines = new List<string>
    {
      $"[bold]{Markup.Escape(hub.Name)}[/] ({hub.Role})  {Markup.Escape(hub.SystemId)}",
      $"[grey]neighbors (≤{AstroEconomyBridge.MaxRangeLy:0} ly bands)[/]",
    };

    foreach (var e in neighbors)
    {
      var name = ids.Bridge.BySystemId.TryGetValue(e.To.Value, out var b) ? b.Name : e.To.Value;
      var role = ids.Bridge.BySystemId.TryGetValue(e.To.Value, out var b2) ? b2.Role.ToString() : "?";
      var hrs = AstroEconomyBridge.TransitHours(e.DistanceLy);
      var days = AstroEconomyBridge.TransitDays(e.DistanceLy);
      lines.Add(
        $"  {e.DistanceLy:0.0} ly / {days:0.#}d ({hrs}h, {e.BandTag}) → {Markup.Escape(Short(name))} ({Markup.Escape(role)})");
    }

    if (ship is not null && ship.Itinerary.LegCount > 0)
    {
      lines.Add("[yellow]active itinerary[/]");
      for (var i = 0; i < ship.Itinerary.LegCount; i++)
      {
        var c = ids.Bridge.Corridors.FirstOrDefault(x => x.Id.Equals(ship.Itinerary.CorridorIds[i]));
        if (c is null)
        {
          continue;
        }

        var from = ids.Bridge.Hubs.FirstOrDefault(h => h.HubId.Equals(c.From))?.Name ?? "?";
        var to = ids.Bridge.Hubs.FirstOrDefault(h => h.HubId.Equals(c.To))?.Name ?? "?";
        var mark = i == ship.LegIndex ? "◆" : "·";
        var d = c.TransitHours / 24.0;
        lines.Add($"  {mark} {Markup.Escape(Short(from))} → {Markup.Escape(Short(to))} ({d:0.#}d / {c.TransitHours}h)");
      }
    }

    return string.Join('\n', lines);
  }

  private static string Short(string name) => name.Length <= 14 ? name : name[..13] + "…";

  private static string Truncate(string s, int max) =>
    string.IsNullOrEmpty(s) ? "" : s.Length <= max ? s : s[..(max - 1)] + "…";
}
