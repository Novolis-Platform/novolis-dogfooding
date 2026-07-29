using Avalonia;
using CalypsoCad.Generation;
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

    /// <summary>Generate CAD companions and optionally export plan/orbit/interior PNGs or a walkthrough via hidden Raylib.</summary>
    private static int RunHeadless(bool exportPng, bool walkthrough)
    {
        var dir = CalypsoRevGGenerator.Generate();
        Console.WriteLine($"Wrote Calypso Rev G CAD companions to:{Environment.NewLine}{dir}");

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
