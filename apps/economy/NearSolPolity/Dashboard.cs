using Novolis.Economy;
using Novolis.Economy.Accounting;
using Novolis.Economy.Logistics;
using Novolis.Economy.Markets;
using Novolis.Economy.Production;
using Novolis.Economy.Simulation;
using Spectre.Console;

namespace NearSolPolity;

internal static class Dashboard
{
  public static Table Build(
    EconomySimulation sim,
    PolityWorld.Ids ids,
    NearSolAgents.Bundle agents,
    CreditCirculation credits,
    IReadOnlyCollection<string> log,
    bool running,
    int hoursPerPulse,
    int pulseMs)
  {
    var world = sim.State.World;
    var stats = world.TransportStats;
    var households = world.Cohorts.Sum(c => c.BudgetRemaining.Amount);
    var ship = world.Shipments.FirstOrDefault(s =>
      !s.IsLegacy && s.FirmId.Equals(ids.Carrier) && s.Status == ShipmentStatus.InTransit);
    var openOrders = world.HubOrders.Count(o => !o.IsFilled);
    var fills = credits.BookFills;
    var carrier = agents.Carrier;

    var root = new Table().Border(TableBorder.Rounded).Expand();
    root.AddColumn(new TableColumn("[steelblue1]Near-Sol Tycoon[/]").Width(48));
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
    left.AddRow("Liquid $", $"[bold]{credits.LiquidStock:0}[/]");
    left.AddRow("Households", $"{households:0}");
    left.AddRow("Wages→hh", $"{credits.WagesDistributed:0}");
    left.AddRow("Imports", $"{credits.ImportSpend:0}");
    left.AddRow("Loans", $"{credits.ActiveLoans} active  Δ{credits.InterestPaid:0.#}");
    left.AddRow("Book", $"open {openOrders}  fills {fills}");
    foreach (var (name, firmId) in ids.Firms)
    {
      var ledger = world.Ledgers[firmId];
      left.AddRow(name, $"cash {ledger.Cash.Amount:0}  rev {Math.Abs(ledger.Balance(AccountRole.Revenue).Amount):0}");
    }

    left.AddRow("Delivered", $"{stats.CargoDelivered.Value:0}  burn {stats.FuelBurned.Value:0.#}");
    left.AddRow("Fails", $"{stats.FailedPlans}");
    left.AddRow("Mining", $"[grey]{Markup.Escape(agents.Mining.LastDecision)}[/]");
    left.AddRow("Industry", $"[grey]{Markup.Escape(agents.Industry.LastDecision)}[/]");
    left.AddRow("Station", $"[grey]{Markup.Escape(agents.Station.LastDecision)}[/]");
    left.AddRow("Carrier", $"[yellow]{Markup.Escape(carrier.LastDecision)}[/]");
    left.AddRow("Treasury", $"[grey]{Markup.Escape(agents.Treasury.LastDecision)}[/]");
    left.AddRow("Eval", $"[grey]{Markup.Escape(Truncate(carrier.LastEval, 70))}[/]");
    left.AddRow("Highlights", Markup.Escape(StockHighlights(sim, ids)));

    if (ship is null)
    {
      var hubName = world.Hubs.TryGetValue(carrier.CurrentHub, out var h) ? h.Name : "?";
      left.AddRow("Hull", $"[grey]docked @ {Markup.Escape(hubName)}[/]");
    }
    else
    {
      var hubName = world.Hubs.TryGetValue(ship.CurrentHubId, out var h) ? h.Name : "?";
      left.AddRow("Hull", $"[aqua]{ship.Phase}[/] @ {Markup.Escape(hubName)}");
      left.AddRow("Leg", $"{ship.LegIndex}/{ship.Itinerary.LegCount}  seg {ship.SegmentHoursRemaining}h");
    }

    var map = new Panel(BuildRouteStrip(ids, ship, carrier.CurrentHub))
    {
      Header = new PanelHeader(" Route strip "),
      Border = BoxBorder.Square,
    };
    var journal = new Panel(string.Join('\n', log.Reverse().Select(l => $"[grey]{Markup.Escape(l)}[/]")))
    {
      Header = new PanelHeader(" Log "),
      Border = BoxBorder.Rounded,
      Height = 14,
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
        Ore: world.Inventory.GetQuantity(new InventoryKey(ids.Mining, s.Hub.LocationId, ids.Ore)).Value))
      .OrderByDescending(x => x.Ore)
      .Take(2)
      .Select(x => $"M:{x.Name} raw{x.Ore:0}");

    var plants = ids.Sites.Values
      .Where(s => s.Hub.Role == SystemRole.Industrial)
      .Select(s => (
        Name: Short(s.Hub.Name),
        Parts: world.Inventory.GetQuantity(new InventoryKey(ids.Industry, s.Hub.LocationId, ids.Parts)).Value,
        Goods: world.Inventory.GetQuantity(new InventoryKey(ids.Industry, s.Hub.LocationId, ids.Goods)).Value))
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
    var names = ids.Bridge.Hubs
      .OrderBy(h => h.Name, StringComparer.Ordinal)
      .Take(12)
      .Select(h => h.HubId.Equals(focus) ? $"[bold aqua]{Short(h.Name)}[/]" : $"[grey]{Short(h.Name)}[/]");
    return string.Join(" · ", names);
  }

  private static string Short(string name) =>
    name.Length <= 10 ? name : name[..8] + "…";

  private static string Truncate(string text, int max) =>
    text.Length <= max ? text : text[..(max - 1)] + "…";
}
