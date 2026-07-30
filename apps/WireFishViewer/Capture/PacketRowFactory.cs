using Novolis.Transports.WireFish;

namespace WireFishViewer.Capture;

public static class PacketRowFactory
{
    public static PacketRow FromDevicePacket(DevicePacket packet, int number)
    {
        return new PacketRow
        {
            Number = number,
            Time = packet.Timestamp.ToLocalTime().ToString("HH:mm:ss.ffffff"),
            Source = packet.GetSourceDisplay(),
            Destination = packet.GetDestinationDisplay(),
            Protocol = packet.GetProtocolName(),
            Length = packet.GetPacketLength(),
            Info = packet.FormatInfoLine(),
            RawBytes = packet.GetRawBytes(),
            LinkLayerType = packet.GetLinkLayerType(),
            DeviceName = packet.GetDeviceName(),
        };
    }
}
