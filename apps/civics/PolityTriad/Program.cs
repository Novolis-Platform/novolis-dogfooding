using System.Diagnostics;
using Novolis.Economy.Core;
using Novolis.Geopolitics.Core;
using PolityTriad;
using Spectre.Console;

var headlessMonths = TryParseHeadless(args, out var months);
if (!headlessMonths && (Console.IsOutputRedirected || Console.IsInputRedirected))
{
    headlessMonths = !args.Any(a => a.Equals("--live", StringComparison.OrdinalIgnoreCase));
    months = 36;
}

var model = TriadWorld.Create();
var log = new Queue<string>();
var running = true;
var pulseMs = 350;
log.Enqueue("Theatre online — α/β industrial Economies → Civics delivery; γ geo civic; trade/treaties/war.");

if (headlessMonths)
{
    var sw = Stopwatch.StartNew();
    for (var i = 0; i < months; i++)
    {
        MonthTick.RunScriptedArc(model, log, i);
        MonthTick.Advance(model, log);
        if ((i + 1) % 6 == 0)
            Console.Error.WriteLine($"… month {i + 1}/{months} [{model.Phase}]");
    }

    sw.Stop();
    Console.Error.WriteLine();
    WriteHeadlessReport(model, months, sw.Elapsed);
    return;
}

AnsiConsole.Write(new FigletText("Polity Triad").Color(Color.SteelBlue1));
AnsiConsole.MarkupLine(
    "[grey]Economy periods → Civics delivery → Geopolitics trade/treaties/conflict on a 3-polity frontier.[/]");
AnsiConsole.MarkupLine(
    "[grey]Keys:[/] [bold]1–4[/] speed  [bold]Space[/] pause  [bold]W[/] war  [bold]P[/] peace  " +
    "[bold]C[/] CM  [bold]R[/] R&D  [bold]A[/] agents  [bold]Q[/] quit");
AnsiConsole.MarkupLine("[grey]Headless:[/] [bold]--headless 36[/] (scripted CM → R&D → war → peace)");

await AnsiConsole.Live(Dashboard.Build(model, log, running, pulseMs))
    .AutoClear(false)
    .Overflow(VerticalOverflow.Ellipsis)
    .StartAsync(async ctx =>
    {
        while (true)
        {
            ctx.UpdateTarget(Dashboard.Build(model, log, running, pulseMs));

            while (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true);
                if (!HandleKey(key))
                {
                    ctx.UpdateTarget(Dashboard.Build(model, log, running, pulseMs));
                    return;
                }
            }

            if (running)
                MonthTick.Advance(model, log);

            await Task.Delay(pulseMs);
        }
    });

AnsiConsole.MarkupLine("[green]Done.[/] Day [bold]{0}[/] phase [bold]{1}[/]", model.World.Day, model.Phase);
return;

bool HandleKey(ConsoleKeyInfo key)
{
    switch (key.Key)
    {
        case ConsoleKey.Q:
            return false;
        case ConsoleKey.Spacebar:
            running = !running;
            log.Enqueue(running ? "resumed" : "paused");
            break;
        case ConsoleKey.D1:
            pulseMs = 800;
            log.Enqueue("speed slow");
            break;
        case ConsoleKey.D2:
            pulseMs = 350;
            log.Enqueue("speed 1×");
            break;
        case ConsoleKey.D3:
            pulseMs = 120;
            log.Enqueue("speed fast");
            break;
        case ConsoleKey.D4:
            pulseMs = 40;
            log.Enqueue("speed blitz");
            break;
        case ConsoleKey.W:
            MonthTick.DeclareWar(model, log);
            break;
        case ConsoleKey.P:
            MonthTick.OfferPeace(model, log);
            break;
        case ConsoleKey.C:
            MonthTick.SignCommonMarket(model, log);
            break;
        case ConsoleKey.R:
            MonthTick.SignResearch(model, log);
            break;
        case ConsoleKey.A:
            model.AgentsEnabled = !model.AgentsEnabled;
            log.Enqueue(model.AgentsEnabled ? "fiscal agents ON" : "fiscal agents OFF");
            break;
    }

    return true;
}

static bool TryParseHeadless(string[] args, out int months)
{
    months = 36;
    for (var i = 0; i < args.Length; i++)
    {
        if (!args[i].Equals("--headless", StringComparison.OrdinalIgnoreCase))
            continue;
        if (i + 1 < args.Length && int.TryParse(args[i + 1], out var n) && n > 0)
            months = n;
        return true;
    }

    return false;
}

static void WriteHeadlessReport(TriadWorld.Model model, int months, TimeSpan elapsed)
{
    var alpha = model.World.Polity(new PolityId(0));
    var beta = model.World.Polity(new PolityId(1));
    var gamma = model.World.Polity(new PolityId(2));
    var aCash = model.AlphaEconomy.Entities.Values.Sum(e => e.Cash.Amount);
    var bCash = model.BetaEconomy.Entities.Values.Sum(e => e.Cash.Amount);
    var occupied = model.World.Provinces.Count(p => p.OwnerId != p.HomePolityId);

    Console.WriteLine("=== Polity Triad headless (intricate theatre) ===");
    Console.WriteLine($"Months: {months}  wall {elapsed.TotalSeconds:0.0}s  phase={model.Phase}  captures={model.CapturesTotal}");
    Console.WriteLine(
        $"Alpha L/A/WF/HD: {alpha.Civic.Legitimacy:0.00}/{alpha.Civic.Approval:0.00}/" +
        $"{alpha.Civic.WarFatigue:0.00}/{alpha.Civic.HumanDevelopment:0.00}  " +
        $"GDP {alpha.Gdp:0}  force {alpha.Military.Total:0.#}  state {model.AlphaEconomy.Entities[TriadWorld.AlphaState].Cash.Amount:0.#}");
    Console.WriteLine(
        $"Beta  L/A/WF/HD: {beta.Civic.Legitimacy:0.00}/{beta.Civic.Approval:0.00}/" +
        $"{beta.Civic.WarFatigue:0.00}/{beta.Civic.HumanDevelopment:0.00}  " +
        $"GDP {beta.Gdp:0}  force {beta.Military.Total:0.#}  state {model.BetaEconomy.Entities[TriadWorld.BetaState].Cash.Amount:0.#}");
    Console.WriteLine(
        $"Gamma L/A/WF: {gamma.Civic.Legitimacy:0.00}/{gamma.Civic.Approval:0.00}/{gamma.Civic.WarFatigue:0.00}  " +
        $"tech {gamma.TechLevel:0.00}  GDP {gamma.Gdp:0}");
    Console.WriteLine(
        $"Economy cash αΣ {aCash:0.#} βΣ {bCash:0.#}  " +
        $"at war: {model.World.AreAtWar(new PolityId(0), new PolityId(1))}  occupied provinces: {occupied}");
    Console.WriteLine(
        $"Trade CMΣ {model.Telemetry.CommonMarketVolume:0.#}  worldΣ {model.Telemetry.WorldMarketVolume:0.#}  " +
        $"CM treaties {model.World.CountActiveTreatiesOfKind(TreatyKind.CommonMarket)}  " +
        $"R&D {model.World.CountActiveTreatiesOfKind(TreatyKind.ResearchPartnership)}");
    if (model.History.AlphaWarFatigue.Count > 0)
    {
        Console.WriteLine(
            $"Arc αL {TriadHistory.Spark(model.History.AlphaLegitimacy)}  " +
            $"αWF {TriadHistory.Spark(model.History.AlphaWarFatigue)}  " +
            $"βWF {TriadHistory.Spark(model.History.BetaWarFatigue)}");
    }
}
