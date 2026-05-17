using System.Net;
using System.Net.NetworkInformation;
using PacketDotNet;
using TUnit.Core;
using WireFishViewer.Capture;

namespace WireFishViewer.Tests;

public class PacketDetailBuilderTests
{
    [Test]
    public async Task Build_tcpOverIpv4_has_ethernet_ip_tcp_layers()
    {
        var eth = new EthernetPacket(
            PhysicalAddress.Parse("001122334455"),
            PhysicalAddress.Parse("AABBCCDDEEFF"),
            EthernetType.IPv4)
        {
            PayloadPacket = new IPv4Packet(IPAddress.Parse("192.168.1.1"), IPAddress.Parse("192.168.1.2"))
            {
                PayloadPacket = new TcpPacket(1234, 443)
                {
                    PayloadData = [],
                },
            },
        };

        var row = new PacketRow
        {
            Number = 1,
            Time = "00:00:00",
            Source = "192.168.1.1",
            Destination = "192.168.1.2",
            Protocol = "Tcp",
            Length = eth.Bytes.Length,
            Info = "test",
            RawBytes = eth.Bytes,
            LinkLayerType = LinkLayers.Ethernet,
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
