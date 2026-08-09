using Avalonia;
using CalypsoCad.Generation;
using CalypsoCad.Models;
using CalypsoCad.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CalypsoCad;

internal static class Program
{
    internal static IHost ApplicationHost { get; private set; } = null!;

    [STAThread]
    public static void Main(string[] args)
    {
        var generateOnly = args.Any(a => string.Equals(a, "--generate-only", StringComparison.OrdinalIgnoreCase));
        var headless = args.Any(a => string.Equals(a, "--headless", StringComparison.OrdinalIgnoreCase));
        var walkthrough = args.Any(a => string.Equals(a, "--walkthrough", StringComparison.OrdinalIgnoreCase));
        var jsonOnly = args.Any(a => string.Equals(a, "--json-only", StringComparison.OrdinalIgnoreCase));
        var acceptance = args.Any(a => string.Equals(a, "--acceptance", StringComparison.OrdinalIgnoreCase));

        if (acceptance)
        {
            Environment.ExitCode = RunAcceptance();
            return;
        }

        if (generateOnly || headless || walkthrough)
        {
            // --generate-only: companions only (PNG skipped unless also --headless)
            // --headless: still PNG tour
            // --walkthrough: PNG frame sequence + ffmpeg MP4/GIF when available
            Environment.ExitCode = RunHeadless(
                exportPng: headless && !jsonOnly,
                walkthrough: walkthrough);
            return;
        }

        ApplicationHost = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddSingleton<CalypsoSession>();
                services.AddSingleton<CalypsoRenderer>();
                services.AddTransient<MainWindow>();
            })
            .Build();

        ApplicationHost.Start();

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            ApplicationHost.StopAsync().GetAwaiter().GetResult();
        }
    }

    /// <summary>Seed generate → inject exterior → regenerate → assert exterior preserved + lock metrics.</summary>
    private static int RunAcceptance()
    {
        Console.WriteLine("CalypsoCad acceptance");
        var failures = 0;
        void Check(string name, bool ok, string detail = "")
        {
            if (ok)
            {
                Console.WriteLine($"  OK  {name}");
                return;
            }

            failures++;
            Console.WriteLine($"  FAIL {name}{(string.IsNullOrEmpty(detail) ? "" : ": " + detail)}");
        }

        var dir = CalypsoLockGenerator.Generate();
        var cadPath = Path.Combine(dir, "calypso.cadjson");
        Check("generate calypso.cadjson", File.Exists(cadPath));

        var doc = System.Text.Json.JsonSerializer.Deserialize<Novolis.Cad.Primitives.CadDocument>(
            File.ReadAllText(cadPath), CadJson.Options)
            ?? throw new InvalidOperationException("Failed to deserialize calypso.cadjson");

        var loa = Novolis.Ship.Primitives.ShipDocumentMetrics.GetLoaMeters(doc);
        Check("LOA 69", Math.Abs(loa - 69f) < 0.01f, $"got {loa}");

        var cabins = Novolis.Ship.Primitives.ShipCad.Spaces(doc)
            .Where(s => s.Name is not null && s.Name.StartsWith("CABIN_", StringComparison.OrdinalIgnoreCase))
            .Select(s => s.Name!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        Check("five crew cabins", cabins.Count == 5, string.Join(",", cabins));

        var corrP = Novolis.Ship.Primitives.ShipCad.Spaces(doc)
            .FirstOrDefault(s => s.Name == "CORR_P" && s.Deck == 0);
        if (corrP?.Points is { Count: >= 4 } pts)
        {
            var xs = pts.Select(p => p[0]).ToList();
            var clear = xs.Max() - xs.Min();
            Check("CORR_P clear ≥ 2 m", clear >= 1.99f, $"got {clear:0.###}");
        }
        else
        {
            Check("CORR_P present", false);
        }

        Check("structure mass attached", Novolis.Ship.Structure.ShipStructureDocument.TryGetMass(doc, out var mass)
                                         && mass is not null && mass.MassKg > 240_000f);

        var d3 = Novolis.Ship.Primitives.ShipCad.Openings(doc)
            .FirstOrDefault(o => o.Name is not null && o.Name.StartsWith("D3-", StringComparison.OrdinalIgnoreCase));
        Check("D3 vacuum-assisted", d3 is not null && Novolis.Ship.Primitives.ShipCad.IsVacuumAssisted(d3!));

        var topo = Novolis.Ship.Topology.ShipTopology.Analyze(doc);
        Check("topology has spaces", topo.SpaceIds.Count > 0, $"count={topo.SpaceIds.Count}");
        Novolis.Ship.Topology.ShipTopology.ApplySpaceFlags(doc, topo);

        var validation = Novolis.Ship.Validation.ShipValidator.Validate(doc, topo);
        var hardErrors = validation.Issues
            .Where(i => i.Severity == Novolis.Ship.Validation.ShipValidationSeverity.Error)
            .ToList();
        // Orphan openings / clear-width must not fail the lock seed; report only true Errors.
        Check("validator no errors", hardErrors.Count == 0,
            string.Join("; ", hardErrors.Select(i => $"{i.Code}:{i.Message}")));

        doc.Entities.Add(new Novolis.Cad.Primitives.CadEntity
        {
            Kind = "box",
            Name = "ext-acceptance-hull",
            Center = [0, 6, 32],
            HalfExtents = [10f, 3f, 5f],
            Properties = new Dictionary<string, System.Text.Json.JsonElement>
            {
                ["exterior"] = System.Text.Json.JsonSerializer.SerializeToElement(true),
            },
        });
        File.WriteAllText(cadPath, System.Text.Json.JsonSerializer.Serialize(doc, CadJson.Options));

        CalypsoLockGenerator.Generate();
        var after = System.Text.Json.JsonSerializer.Deserialize<Novolis.Cad.Primitives.CadDocument>(
            File.ReadAllText(cadPath), CadJson.Options)
            ?? throw new InvalidOperationException("Failed to deserialize after regenerate");
        Check(
            "regenerate preserves exterior",
            after.Entities.Any(e => string.Equals(e.Name, "ext-acceptance-hull", StringComparison.OrdinalIgnoreCase)));

        Console.WriteLine(failures == 0 ? "CalypsoCad acceptance OK" : $"CalypsoCad acceptance FAILED ({failures})");
        return failures == 0 ? 0 : 1;
    }

    /// <summary>Generate CAD companions and optionally export plan/orbit/interior PNGs or a walkthrough via hidden Raylib.</summary>
    private static int RunHeadless(bool exportPng, bool walkthrough)
    {
        var dir = CalypsoRevGGenerator.Generate();
        Console.WriteLine($"Wrote Calypso Rev H (lock LOA 69) CAD companions to:{Environment.NewLine}{dir}");

        if (!exportPng && !walkthrough)
            return 0;

        try
        {
            var session = new CalypsoSession();
            session.RegenerateAndLoad();
            var renderer = new CalypsoRenderer(session);
            renderer.Fit();
            renderer.SyncInteriorFromSelection();

            var saved = new List<string>();

            if (exportPng)
            {
                var stills = HeadlessPngExporter.ExportViews(session, renderer, session.GeneratedDirectory);
                saved.AddRange(stills);
                if (stills.Count == 0)
                {
                    Console.Error.WriteLine("Headless PNG export produced no frames (Raylib/GPU may be unavailable).");
                    return 2;
                }

                Console.WriteLine($"Headless Raylib PNG exports ({stills.Count}):");
                foreach (var path in stills)
                    Console.WriteLine($"  {path}");
            }

            if (walkthrough)
            {
                var wt = HeadlessWalkthroughExporter.Export(session, renderer, session.GeneratedDirectory);
                saved.AddRange(wt);
                if (wt.Count == 0)
                {
                    Console.Error.WriteLine("Walkthrough export produced no frames (Raylib/GPU may be unavailable).");
                    return 2;
                }

                Console.WriteLine($"Walkthrough exports ({wt.Count}):");
                foreach (var path in wt)
                    Console.WriteLine($"  {path}");
            }

            return saved.Count > 0 ? 0 : 2;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Headless export failed: {ex.Message}");
            return 2;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
