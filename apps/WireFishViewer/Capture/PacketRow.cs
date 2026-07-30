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

    /// <summary>Link-layer type integer (e.g. <see cref="Novolis.Transports.WireFish.PacketPresentation.LinkLayerEthernet"/>).</summary>
    public required int LinkLayerType { get; init; }

    public required string DeviceName { get; init; }
}
