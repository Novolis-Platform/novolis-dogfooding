using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace WireFishViewer.Capture;

public interface IPacketStore
{
    ReadOnlyObservableCollection<PacketRow> Packets { get; }

    event NotifyCollectionChangedEventHandler? CollectionChanged;

    void Add(PacketRow row);

    void AddRange(IReadOnlyList<PacketRow> rows);

    void Clear();

    int Count { get; }
}
