using Spectre.Console;

namespace BridgeCommander.Bridge;

/// <summary>Spectre.Console rendering for bridge state and exchange beats.</summary>
public static class BridgeSpectreUi
{
    public static void ShowTitle()
    {
        AnsiConsole.Write(new FigletText("Bridge Commander").Color(Color.Cyan1));
        AnsiConsole.MarkupLine("[grey]Voice exchange · Novolis.Commands dogfood · Ctrl+C to abort[/]");
        AnsiConsole.WriteLine();
    }

    public static void ShowBeat(BridgeExchangeBeat beat)
    {
        var color = beat.Speaker switch
        {
            "Captain" => "yellow",
            "Computer" => "aqua",
            "Executive Officer" => "green",
            _ => "white",
        };

        AnsiConsole.MarkupLine($"[{color} bold]{Markup.Escape(beat.Speaker)}[/]: {Markup.Escape(beat.Display)}");
    }

    public static void RenderBridge(BridgeState state)
    {
        var status = new Panel(Markup.Escape(state.StatusLine))
            .Header("[cyan]Station report[/]")
            .RoundedBorder();

        var ship = new Panel(
            $"[bold]{Markup.Escape(state.ShipName)}[/]\n{Markup.Escape(state.FormatStatus())}")
            .Header("[cyan]Tactical[/]")
            .RoundedBorder();

        var logLines = state.FormatHistory().TakeLast(12).ToArray();
        var logBody = logLines.Length == 0
            ? "[grey](no entries yet)[/]"
            : string.Join('\n', logLines.Select(l => Markup.Escape(l)));

        var log = new Panel(logBody)
            .Header("[cyan]Command log[/]")
            .RoundedBorder()
            .Expand();

        var grid = new Grid();
        grid.AddColumn();
        grid.AddRow(new Columns(ship, status));
        grid.AddRow(log);

        AnsiConsole.Write(grid);
        AnsiConsole.WriteLine();
    }

    public static void ShowClosing()
    {
        AnsiConsole.MarkupLine("[green]Exchange complete.[/] Run with [cyan]--interactive[/] for manual orders.");
    }
}
