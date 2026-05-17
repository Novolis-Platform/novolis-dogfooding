using Novolis.Avalonia.Layout;
using PacketDotNet;

namespace WireFishViewer.Capture;

public static class PacketDetailBuilder
{
    public static DetailTreeNode Build(PacketRow row)
    {
        var packet = Packet.ParsePacket(row.LinkLayerType, row.RawBytes);
        return BuildNode(packet, row.DeviceName);
    }

    private static DetailTreeNode BuildNode(Packet packet, string deviceName)
    {
        var children = new List<DetailTreeNode>();
        if (packet.PayloadPacket is not null)
            children.Add(BuildNode(packet.PayloadPacket, deviceName));

        var description = Describe(packet);
        return new DetailTreeNode($"{packet.GetType().Name} ({packet.TotalPacketLength} bytes)", description, children);
    }

    private static string? Describe(Packet packet) => packet switch
    {
        EthernetPacket eth => $"Src={eth.SourceHardwareAddress} Dst={eth.DestinationHardwareAddress} Type={eth.Type}",
        IPv4Packet ip4 => $"Src={ip4.SourceAddress} Dst={ip4.DestinationAddress} TTL={ip4.TimeToLive} Proto={ip4.Protocol}",
        IPv6Packet ip6 => $"Src={ip6.SourceAddress} Dst={ip6.DestinationAddress} Next={ip6.NextHeader}",
        TcpPacket tcp => $"Ports {tcp.SourcePort}→{tcp.DestinationPort} Seq={tcp.SequenceNumber} Ack={tcp.AcknowledgmentNumber}",
        UdpPacket udp => $"Ports {udp.SourcePort}→{udp.DestinationPort} Len={udp.Length}",
        ArpPacket arp => $"WhoHas {arp.TargetProtocolAddress} Tell {arp.SenderProtocolAddress}",
        _ => packet.ToString(StringOutputType.Verbose).Split('\n').FirstOrDefault(),
    };
}
