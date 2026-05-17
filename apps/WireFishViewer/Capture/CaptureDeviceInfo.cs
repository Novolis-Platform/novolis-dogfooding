namespace WireFishViewer.Capture;

public sealed record CaptureDeviceInfo(string DisplayName, string CaptureKey)
{
    public override string ToString() => DisplayName;
}
