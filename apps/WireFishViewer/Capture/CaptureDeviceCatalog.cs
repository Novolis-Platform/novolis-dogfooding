using Novolis.Transports.WireFish;

namespace WireFishViewer.Capture;

public static class CaptureDeviceCatalog
{
    public static IReadOnlyList<CaptureDeviceInfo> ListDevices() =>
        WireFishCaptureDevices.List()
            .Select(d => new CaptureDeviceInfo(d.DisplayName, d.CaptureKey))
            .ToList();

    public static bool HasCaptureDevices => WireFishCaptureDevices.Any();

    public static WireFishCaptureHealth DriverHealth => WireFishCaptureHealthChecks.Check();
}
