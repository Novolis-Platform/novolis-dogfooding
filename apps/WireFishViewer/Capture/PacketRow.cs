using PacketDotNet;

namespace WireFishViewer.Capture;

public sealed class PacketRow
{
    public required int Number { get; init; }

    public required string Time { get; init; }

    public required string Source { get; init; }

    public required string Destination { get; init; }

    public required string Protocol { get; init; }

    public required int Length { get; init; }

    public required string Info { get; init; }

    public required byte[] RawBytes { get; init; }

    public required LinkLayers LinkLayerType { get; init; }

    public required string DeviceName { get; init; }
}
