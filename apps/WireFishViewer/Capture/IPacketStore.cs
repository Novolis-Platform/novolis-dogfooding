using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace WireFishViewer.Capture;

public interface IPacketStore
{
    ReadOnlyObservableCollection<PacketRow> Packets { get; }

    event NotifyCollectionChangedEventHandler? CollectionChanged;

    void Add(PacketRow row);

    void Clear();

    int Count { get; }
}
