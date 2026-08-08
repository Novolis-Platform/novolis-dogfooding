using Novolis.Avalonia.Controls;
using Novolis.Transports.WireFish;

namespace WireFishViewer.Capture;

public static class PacketDetailBuilder
{
    public static DetailTreeNode Build(PacketRow row)
    {
        var node = PacketPresentation.BuildDetailTree(row.RawBytes, row.LinkLayerType, row.DeviceName);
        return Map(node);
    }

    private static DetailTreeNode Map(PacketDetailNode node) =>
        new(node.Title, node.Description, node.Children.Select(Map).ToList());
}
