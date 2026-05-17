using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace WireFishViewer.Capture;

public sealed class PacketStore : IPacketStore
{
    public const int MaxPackets = 10_000;

    private readonly ObservableCollection<PacketRow> _packets = [];

    public PacketStore()
    {
        Packets = new ReadOnlyObservableCollection<PacketRow>(_packets);
        _packets.CollectionChanged += (_, e) => CollectionChanged?.Invoke(this, e);
    }

    public ReadOnlyObservableCollection<PacketRow> Packets { get; }

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public int Count => _packets.Count;

    public void Add(PacketRow row)
    {
        _packets.Add(row);
        while (_packets.Count > MaxPackets)
            _packets.RemoveAt(0);
    }

    public void Clear() => _packets.Clear();
}
