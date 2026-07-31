using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TorrentLab;

internal static class Program
{
    internal static IHost ApplicationHost { get; private set; } = null!;

    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Any(a => string.Equals(a, "--smoke", StringComparison.OrdinalIgnoreCase)))
        {
            var samples = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "samples"));
            if (!Directory.Exists(samples))
                samples = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "samples"));
            Environment.ExitCode = LocalTransferSmoke.Run(samples);
            return;
        }

        ApplicationHost = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services => services.AddTransient<MainWindow>())
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

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
