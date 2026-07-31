using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Novolis.Transports.WireFish;

namespace WireFishViewer;

/// <summary>
/// Self-elevates on Windows via UAC (<c>runas</c>) so <c>dotnet run</c> still works
/// (unlike a <c>requireAdministrator</c> manifest, which the host cannot launch).
/// </summary>
internal static class WindowsElevation
{
    public const string NoElevateArg = "--no-elevate";

    /// <summary>
    /// If not elevated, starts a new elevated copy of this process and returns <see langword="true"/>
    /// (caller should exit). Returns <see langword="false"/> when already elevated, elevation was
    /// skipped/cancelled, or not on Windows.
    /// </summary>
    public static bool TryRelaunchElevatedAndExit(string[] args)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return false;

        if (args.Any(static a => string.Equals(a, NoElevateArg, StringComparison.OrdinalIgnoreCase)))
            return false;

        if (WireFishCaptureHealthChecks.IsProcessElevated())
            return false;

        var exe = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe))
            return false;

        try
        {
            var start = new ProcessStartInfo
            {
                FileName = exe,
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Environment.CurrentDirectory,
            };
            foreach (var arg in args)
                start.ArgumentList.Add(arg);

            using var process = Process.Start(start);
            return process is not null;
        }
        catch (Win32Exception)
        {
            // User cancelled the UAC prompt — continue unelevated.
            return false;
        }
    }
}
