using NearSolPolity;
using Novolis.Economy;
using Novolis.Economy.Logistics;
using Novolis.Economy.Simulation;
using Spectre.Console;

AnsiConsole.Write(new FigletText("Near-Sol").Color(Color.SteelBlue1));
AnsiConsole.MarkupLine("[grey]Interstellar polity — closed-loop credits (wages→households→retail).[/]");
AnsiConsole.MarkupLine(
  "[grey]Keys:[/] [bold]1[/]=½×  [bold]2[/]=1×  [bold]3[/]=4×  [bold]4[/]=16×  " +
  "[bold]5[/]=64×  [bold]6[/]=Warp  [bold]Space[/]=pause  [bold]Q[/]=quit");

var (sim, ids) = PolityWorld.Create();
var polity = new PolityController(sim, ids);
var tramp = new TrampFleetAutopilot(sim, ids);
var credits = new CreditCirculation(sim, ids);
var log = new Queue<string>();
var eventCursor = sim.State.Events.Count;
var running = true;
var hoursPerPulse = 1;
var pulseMs = 280;
var openingLiquid = credits.LiquidStock;
Note($"Catalog online — {ids.Bridge.Hubs.Count} hubs, liquid {openingLiquid:0}");

if (Console.IsOutputRedirected || Console.IsInputRedirected)
{
  var hours = args.Length > 0 && int.TryParse(args[0], out var h) && h > 0 ? h : 500;
  AnsiConsole.MarkupLine($"[yellow]Non-interactive — advancing {hours}h then exiting.[/]");
  for (var i = 0; i < hours; i++)
  {
    await PulseAsync();
  }

  AnsiConsole.Write(Dashboard.Build(sim, ids, polity, tramp, credits, log, running, hoursPerPulse, pulseMs));
  PrintSummary();
  return;
}

await AnsiConsole.Live(Dashboard.Build(sim, ids, polity, tramp, credits, log, running, hoursPerPulse, pulseMs))
  .AutoClear(false)
  .Overflow(VerticalOverflow.Ellipsis)
  .StartAsync(async ctx =>
  {
    while (true)
    {
      ctx.UpdateTarget(Dashboard.Build(sim, ids, polity, tramp, credits, log, running, hoursPerPulse, pulseMs));

      while (Console.KeyAvailable)
      {
        var key = Console.ReadKey(intercept: true);
        if (!HandleKey(key))
        {
          Note("Clearing docking clamps.");
          ctx.UpdateTarget(Dashboard.Build(sim, ids, polity, tramp, credits, log, running, hoursPerPulse, pulseMs));
          return;
        }
      }

      if (running)
      {
        await PulseAsync();
      }

      await Task.Delay(pulseMs);
    }
  });

PrintSummary();
return;

bool HandleKey(ConsoleKeyInfo key)
{
  switch (key.Key)
  {
    case ConsoleKey.Q:
      return false;
    case ConsoleKey.Spacebar:
      running = !running;
      Note(running ? "resumed" : "paused");
      break;
    case ConsoleKey.D1:
      SetSpeed(1, 550, "½×");
      break;
    case ConsoleKey.D2:
      SetSpeed(1, 280, "1×");
      break;
    case ConsoleKey.D3:
      SetSpeed(1, 70, "4×");
      break;
    case ConsoleKey.D4:
      SetSpeed(4, 50, "16×");
      break;
    case ConsoleKey.D5:
      SetSpeed(16, 40, "64×");
      break;
    case ConsoleKey.D6:
      SetSpeed(48, 30, "Warp");
      break;
  }

  return true;
}

void SetSpeed(int hours, int ms, string label)
{
  hoursPerPulse = hours;
  pulseMs = ms;
  running = true;
  Note($"speed {label} ({hours}h / pulse)");
}

async Task PulseAsync()
{
  polity.Tick();
  tramp.Tick();
  var before = sim.State.Events.Count;
  await sim.AdvanceAsync(SimulationDuration.FromHours(hoursPerPulse));
  credits.ObserveAfterPulse(before);
  Capture(before);
}

void Capture(int before)
{
  var events = sim.State.Events;
  for (var i = Math.Max(before, eventCursor); i < events.Count; i++)
  {
    var line = events[i] switch
    {
      ShipmentDeparted e => $"departed ×{e.Quantity.Value:0}",
      ShipmentLegStarted => "entered corridor",
      ShipmentHubArrived => "hub arrival / dwell",
      ShipmentDelivered e => $"delivered ×{e.Quantity.Value:0}",
      ShipmentPlanFailed e => $"plan failed: {e.Reason}",
      FuelBunkered e => $"bunkered {e.Quantity.Value:0.##}",
      TransportTollPaid e => $"toll {e.Amount.Amount:0.##}",
      GoodsSold e => $"sold ×{e.Quantity.Value:0} → {e.Revenue.Amount:0.##}",
      ProcurementFilled e => $"import ×{e.Quantity.Value:0}",
      BatchProduced e => $"produced {ProductHint(e.ProductId)} ×{e.Quantity.Value:0}",
      WagesPaid e => $"wages paid {e.Amount.Amount:0}",
      _ => null,
    };
    if (line is not null)
    {
      Note(line);
    }
  }

  eventCursor = events.Count;
}

string ProductHint(ProductId p)
{
  if (p.Equals(ids.Ore)) return "ore";
  if (p.Equals(ids.Parts)) return "parts";
  if (p.Equals(ids.Goods)) return "goods";
  if (p.Equals(ids.Fuel)) return "fuel";
  return "sku";
}

void Note(string msg)
{
  log.Enqueue($"h{sim.State.Clock.HourIndex}: {msg}");
  while (log.Count > 12)
  {
    log.Dequeue();
  }
}

void PrintSummary()
{
  var world = sim.State.World;
  var trampCash = world.Ledgers[ids.Tramp].Cash.Amount;
  var polityCash = world.Ledgers[ids.Polity].Cash.Amount;
  var hh = world.Cohorts.Sum(c => c.BudgetRemaining.Amount);
  var produced = sim.State.Events.OfType<BatchProduced>().Sum(e => e.Quantity.Value);
  var sold = sim.State.Events.OfType<GoodsSold>().Sum(e => e.Quantity.Value);
  var delivered = world.TransportStats.CargoDelivered.Value;
  AnsiConsole.MarkupLine(
    "[green]Done.[/] Hash [bold]{0:X16}[/]  liquid {1:0} (open {2:0})  tramp {3:0}  polity {4:0}  hh {5:0}  wages→hh {6:0}  imports {7:0}  sold {8:0}  delivered {9:0}",
    sim.State.Hash,
    credits.LiquidStock,
    openingLiquid,
    trampCash,
    polityCash,
    hh,
    credits.WagesDistributed,
    credits.ImportSpend,
    sold,
    delivered);
}
