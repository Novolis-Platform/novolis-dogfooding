using TUnit.Core;
using WireFishViewer.Capture;

namespace WireFishViewer.Tests;

public class PacketDetailBuilderTests
{
    // Minimal Ethernet + IPv4 + TCP SYN frame (same layout as transports WireFish unit tests).
    private static readonly byte[] TcpOverIpv4 =
    [
        0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x08, 0x00,
        0x45, 0x00, 0x00, 0x28, 0x00, 0x00, 0x00, 0x00, 0x40, 0x06, 0x00, 0x00,
        192, 168, 1, 1,
        192, 168, 1, 2,
        0x04, 0xD2, 0x01, 0xBB, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x50, 0x02, 0x20, 0x00, 0x00, 0x00, 0x00, 0x00
    ];

    [Test]
    public async Task Build_tcpOverIpv4_has_ethernet_ip_tcp_layers()
    {
        var row = new PacketRow
        {
            Number = 1,
            Time = "00:00:00",
            Source = "192.168.1.1",
            Destination = "192.168.1.2",
            Protocol = "Tcp",
            Length = TcpOverIpv4.Length,
            Info = "test",
            RawBytes = TcpOverIpv4,
            LinkLayerType = 1, // Ethernet / DLT_EN10MB
            DeviceName = "test0",
        };

        var root = PacketDetailBuilder.Build(row);
        await Assert.That(root.Title).Contains("EthernetPacket");
        await Assert.That(root.Children.Count).IsEqualTo(1);
        await Assert.That(root.Children[0].Title).Contains("IPv4Packet");
        await Assert.That(root.Children[0].Children.Count).IsEqualTo(1);
        await Assert.That(root.Children[0].Children[0].Title).Contains("TcpPacket");
    }
}
