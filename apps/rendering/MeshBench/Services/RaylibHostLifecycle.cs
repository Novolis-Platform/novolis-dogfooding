using System.Reflection;
using Avalonia.Threading;
using Novolis.Avalonia.Raylib;

namespace MeshBench.Services;

/// <summary>Starts/stops <see cref="RaylibHostControl"/> across GPR versions (with or without <c>SetHostActive</c>).</summary>
internal static class RaylibHostLifecycle
{
    private static readonly MethodInfo? SetHostActiveMethod =
        typeof(RaylibHostControl).GetMethod("SetHostActive", [typeof(bool)]);

    private static readonly MethodInfo? StartHostMethod =
        typeof(RaylibHostControl).GetMethod("StartHost", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo? StopHostMethod =
        typeof(RaylibHostControl).GetMethod("StopHost", BindingFlags.Instance | BindingFlags.NonPublic);

    public static void SetActive(RaylibHostControl host, bool active)
    {
        if (SetHostActiveMethod is not null)
        {
            SetHostActiveMethod.Invoke(host, [active]);
            return;
        }

        if (active)
            StartLegacy(host);
        else
            StopLegacy(host);
    }

    private static void StartLegacy(RaylibHostControl host)
    {
        if (host.IsHostRunning)
            return;

        StartHostMethod?.Invoke(host, null);
        StartPresentTimer(host);
    }

    private static void StopLegacy(RaylibHostControl host)
    {
        StopPresentTimer(host);
        StopHostMethod?.Invoke(host, null);
    }

    private static void StartPresentTimer(RaylibHostControl host)
    {
        var field = typeof(RaylibHostControl).GetField("_presentTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field?.GetValue(host) is DispatcherTimer timer)
            timer.Start();
    }

    private static void StopPresentTimer(RaylibHostControl host)
    {
        var field = typeof(RaylibHostControl).GetField("_presentTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field?.GetValue(host) is DispatcherTimer timer)
            timer.Stop();
    }
}
