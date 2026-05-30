using System.Reflection;
using Avalonia.Threading;
using Novolis.Avalonia.Raylib;

namespace MeshBench.Services;

/// <summary>Raylib host helpers across GPR versions (with or without channel/on-demand APIs).</summary>
internal static class RaylibHostBridge
{
    private static readonly MethodInfo? SetHostActiveMethod =
        typeof(RaylibHostControl).GetMethod("SetHostActive", [typeof(bool)]);

    private static readonly MethodInfo? EnsureHostStartedMethod =
        typeof(RaylibHostControl).GetMethod("EnsureHostStarted", Type.EmptyTypes);

    private static readonly MethodInfo? RequestFrameMethod =
        typeof(RaylibHostControl).GetMethod("RequestFrame", Type.EmptyTypes);

    private static readonly PropertyInfo? LastFrameAgeMsProperty =
        typeof(RaylibHostControl).GetProperty("LastFrameAgeMs");

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

    public static void EnsureHostStarted(RaylibHostControl host)
    {
        if (EnsureHostStartedMethod is not null)
        {
            EnsureHostStartedMethod.Invoke(host, null);
            return;
        }

        SetActive(host, true);
    }

    public static void RequestFrame(RaylibHostControl host)
    {
        if (RequestFrameMethod is not null)
            RequestFrameMethod.Invoke(host, null);
    }

    public static double LastFrameAgeMs(RaylibHostControl host) =>
        LastFrameAgeMsProperty?.GetValue(host) is double age ? age : -1;

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
