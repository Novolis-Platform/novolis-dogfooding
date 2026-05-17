using SharpPcap;
using SharpPcap.LibPcap;

namespace WireFishViewer.Capture;

public static class CaptureDeviceCatalog
{
    public static IReadOnlyList<CaptureDeviceInfo> ListDevices()
    {
        try
        {
            return CaptureDeviceList.Instance
                .OfType<LibPcapLiveDevice>()
                .Select(d => new CaptureDeviceInfo(
                    string.IsNullOrWhiteSpace(d.Description) ? d.Name : $"{d.Description} ({d.Name})",
                    d.Name))
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    public static bool HasCaptureDevices => ListDevices().Count > 0;
}
