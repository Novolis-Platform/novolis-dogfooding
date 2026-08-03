using Avalonia;
using Novolis.Audio.Midi;

namespace MusicMakerLab;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
ScorePdfExporter.EnsureCommunityLicense();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            Environment.ExitCode = 1;
        }
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
