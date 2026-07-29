using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Novolis.Agent.Surface;
using Novolis.Avalonia._3D.Session;
using Novolis.Avalonia._3D.Ui;
using Novolis.Modeling.Scene;

namespace SceneLab;

internal static class Program
{
    internal static IHost ApplicationHost { get; private set; } = null!;
    internal static AgentSurface? SceneSurface { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        ApplicationHost = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddSingleton(_ =>
                {
                    var sample = args.Any(a => a.Equals("--cloner", StringComparison.OrdinalIgnoreCase))
                        ? SceneDocument.CreateClonerRow()
                        : args.Any(a => a.Equals("--boole", StringComparison.OrdinalIgnoreCase))
                            ? SceneDocument.CreateBooleCut()
                            : args.Any(a => a.Equals("--look", StringComparison.OrdinalIgnoreCase))
                                ? SceneDocument.CreateLookSetup()
                                : args.Any(a => a.Equals("--edit", StringComparison.OrdinalIgnoreCase))
                                    ? SceneDocument.CreateEditBox()
                                    : args.Any(a => a.Equals("--gallery", StringComparison.OrdinalIgnoreCase))
                                        ? SceneDocument.CreatePrimitiveGallery()
                                        : args.Any(a => a.Equals("--corvette", StringComparison.OrdinalIgnoreCase))
                                            ? LoadCorvetteOrFallback()
                                            : SceneDocument.CreatePrimitiveStage("SceneLab");
                    return new SceneSessionService(sample) { AppId = "scenelab" };
                });
                services.AddTransient<MainWindow>();
            })
            .Build();

        ApplicationHost.Start();
        try
        {
            var session = ApplicationHost.Services.GetRequiredService<SceneSessionService>();
            SceneSurface = AgentSurface.AttachAll(session, session.Definition)
                           ?? AgentSurface.TryAttachFromEnvironment(session, session.Definition);
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            if (SceneSurface is not null)
                SceneSurface.DisposeAsync().AsTask().GetAwaiter().GetResult();
            ApplicationHost.StopAsync().GetAwaiter().GetResult();
        }
    }

    private static SceneDocument LoadCorvetteOrFallback()
    {
        foreach (var candidate in CorvetteSampleCandidates())
        {
            if (File.Exists(candidate))
                return SceneSerializer.Load(candidate);
        }

        return SceneDocument.CreatePrimitiveStage("Troop Corvette (missing sample — run TroopCorvetteBuilder)");
    }

    private static IEnumerable<string> CorvetteSampleCandidates()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "samples", "troop-corvette.nov3djson");
        yield return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "samples", "troop-corvette.nov3djson"));
        yield return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "apps", "avalonia", "SceneLab", "samples", "troop-corvette.nov3djson"));
        // Workspace checkout
        var cwd = Directory.GetCurrentDirectory();
        yield return Path.Combine(cwd, "apps", "avalonia", "SceneLab", "samples", "troop-corvette.nov3djson");
        yield return Path.Combine(cwd, "samples", "troop-corvette.nov3djson");
        yield return @"D:\novolis\novolis-dogfooding\apps\avalonia\SceneLab\samples\troop-corvette.nov3djson";
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}

internal sealed class MainWindow : Window
{
    public MainWindow(SceneSessionService session)
    {
        Title = "Novolis SceneLab — mesh modeller";
        Width = 1600;
        Height = 920;
        MinWidth = 1100;
        MinHeight = 640;
        Background = new SolidColorBrush(Color.FromRgb(14, 20, 28));

        // Factory: surface builds chrome; host docks into C4D-ish layout.
        var surface = new SceneEditorSurface(session, composeDefaultLayout: false);

        var topTools = new StackPanel
        {
            Children =
            {
                Row(surface.EditModeBar, surface.DisplayModeBar, surface.TransformHud),
                Row(surface.PrimitivePalette),
                Row(surface.GeneratorTools, surface.MeshEditTools),
                Row(surface.LookTools),
            },
        };

        var rightRail = new ScrollViewer
        {
            Width = 300,
            Content = new StackPanel
            {
                Children =
                {
                    surface.MeshAttributes,
                    surface.ModifierStack,
                    surface.Properties,
                },
            },
        };

        var center = new Grid { ColumnDefinitions = new ColumnDefinitions("260,*,300") };
        Grid.SetColumn(surface.ObjectManager, 0);
        Grid.SetColumn(surface.Viewport, 1);
        Grid.SetColumn(rightRail, 2);
        center.Children.Add(surface.ObjectManager);
        center.Children.Add(surface.Viewport);
        center.Children.Add(rightRail);

        var sessionLine = new TextBlock
        {
            Margin = new Thickness(10, 2),
            FontSize = 11,
            Opacity = 0.75,
            Foreground = Brushes.WhiteSmoke,
            Text = Program.SceneSurface?.HttpBaseUrl is { } url
                ? $"Session HTTP {url}  TCP :{Program.SceneSurface.TcpPort}"
                : "Session not attached (set NOVOLIS_SCENE_SESSION=1).",
        };

        var bottom = new StackPanel
        {
            [DockPanel.DockProperty] = Dock.Bottom,
            Children = { surface.StatusBar, sessionLine },
        };

        var chrome = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(22, 32, 42)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(40, 60, 75)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = topTools,
            [DockPanel.DockProperty] = Dock.Top,
        };

        Content = new DockPanel
        {
            Background = new SolidColorBrush(Color.FromRgb(14, 20, 28)),
            Children = { chrome, bottom, center },
        };

        Opened += (_, _) => surface.StartPresenting();
        Closed += (_, _) => surface.StopPresenting();
    }

    private static Border Row(params Control[] children)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var child in children)
            row.Children.Add(child);
        return new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(40, 60, 75)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = row,
        };
    }
}
