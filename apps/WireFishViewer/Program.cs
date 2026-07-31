using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Novolis.Transports.WireFish;
using WireFishViewer.Capture;

namespace WireFishViewer;

internal static class Program
{
    internal static IHost ApplicationHost { get; private set; } = null!;

    [STAThread]
    public static void Main(string[] args)
    {
        // Manifest is asInvoker so `dotnet run` can spawn us; then we UAC-relaunch ourselves.
        if (WindowsElevation.TryRelaunchElevatedAndExit(args))
            return;

        _ = WireFishCaptureHealthChecks.TryEnsureCaptureDriver();

        ApplicationHost = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddSingleton<IPacketStore, PacketStore>();
                services.AddSingleton<UiPacketCaptureHandler>();
                services.AddSingleton<CaptureSessionService>();
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

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
