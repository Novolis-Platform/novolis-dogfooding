using Novolis.Transports.WireFish;
using PacketDotNet;

namespace WireFishViewer.Capture;

public static class PacketRowFactory
{
    public static PacketRow FromDevicePacket(DevicePacket packet, int number)
    {
        var source = packet.GetSourceIPAddress()?.ToString() ?? packet.GetMacSourceAddress()?.ToString() ?? "-";
        var destination = packet.GetDestinationIPAddress()?.ToString() ?? packet.GetMacDestinationAddress()?.ToString() ?? "-";
        var protocol = packet.GetProtocol().ToString();
        var info = PacketSummaryFormatter.Format(packet);

        return new PacketRow
        {
            Number = number,
            Time = packet.Timestamp.ToLocalTime().ToString("HH:mm:ss.ffffff"),
            Source = source,
            Destination = destination,
            Protocol = protocol,
            Length = packet.GetPacketLength(),
            Info = info,
            RawBytes = packet.Packet.Bytes,
            LinkLayerType = LinkLayers.Ethernet,
            DeviceName = packet.Device.Name,
        };
    }
}
