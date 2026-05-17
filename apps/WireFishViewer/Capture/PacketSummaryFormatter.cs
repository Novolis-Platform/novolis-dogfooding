using Novolis.Transports.WireFish;
using PacketDotNet;

namespace WireFishViewer.Capture;

internal static class PacketSummaryFormatter
{
    public static string Format(DevicePacket packet)
    {
        if (packet.IsTcp())
        {
            var tcp = packet.Packet.Extract<TcpPacket>();
            if (tcp is not null)
                return $"TCP {tcp.SourcePort} → {tcp.DestinationPort} [{FormatTcpFlags(tcp)}] Seq={tcp.SequenceNumber} Ack={tcp.AcknowledgmentNumber} Win={tcp.WindowSize}";
        }

        if (packet.IsUdp())
        {
            var udp = packet.Packet.Extract<UdpPacket>();
            if (udp is not null)
                return $"UDP {udp.SourcePort} → {udp.DestinationPort} Len={udp.Length}";
        }

        if (packet.IsArpPacket())
            return "ARP";

        if (packet.IsDnsPacket())
            return "DNS";

        var summary = packet.GetPacketSummary();
        var firstLine = summary.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return firstLine?.Length > 120 ? firstLine[..120] + "…" : firstLine ?? packet.Packet.GetType().Name;
    }

    private static string FormatTcpFlags(TcpPacket tcp)
    {
        var flags = new List<string>(4);
        if (tcp.Synchronize) flags.Add("SYN");
        if (tcp.Acknowledgment) flags.Add("ACK");
        if (tcp.Finished) flags.Add("FIN");
        if (tcp.Reset) flags.Add("RST");
        if (tcp.Push) flags.Add("PSH");
        return flags.Count == 0 ? "·" : string.Join(",", flags);
    }
}
