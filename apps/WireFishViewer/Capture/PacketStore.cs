using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace WireFishViewer.Capture;

/// <summary>Observable list that supports bulk append without Reset (keeps DataGrid selection).</summary>
internal sealed class PacketRowCollection : ObservableCollection<PacketRow>
{
    private bool _suppress;

    public void AddRange(IReadOnlyList<PacketRow> rows)
    {
        if (rows.Count == 0)
            return;

        var startIndex = Items.Count;
        _suppress = true;
        try
        {
            foreach (var row in rows)
                Items.Add(row);
        }
        finally
        {
            _suppress = false;
        }

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        // Add (not Reset) so selection/scroll survive live capture.
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(
            NotifyCollectionChangedAction.Add,
            (IList)(rows as IList ?? rows.ToList()),
            startIndex));
    }

    public void TrimFrontTo(int maxCount)
    {
        if (Count <= maxCount)
            return;

        var remove = Count - maxCount;
        _suppress = true;
        try
        {
            for (var i = 0; i < remove; i++)
                Items.RemoveAt(0);
        }
        finally
        {
            _suppress = false;
        }

        // Trim is rare at the cap; Reset is acceptable here.
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!_suppress)
            base.OnCollectionChanged(e);
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (!_suppress)
            base.OnPropertyChanged(e);
    }
}

public sealed class PacketStore : IPacketStore
{
    public const int MaxPackets = 5_000;

    private readonly PacketRowCollection _packets = [];

    public PacketStore()
    {
        Packets = new ReadOnlyObservableCollection<PacketRow>(_packets);
        ((INotifyCollectionChanged)_packets).CollectionChanged += (_, e) => CollectionChanged?.Invoke(this, e);
    }

    public ReadOnlyObservableCollection<PacketRow> Packets { get; }

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public int Count => _packets.Count;

    public void Add(PacketRow row) => AddRange([row]);

    public void AddRange(IReadOnlyList<PacketRow> rows)
    {
        if (rows.Count == 0)
            return;

        _packets.AddRange(rows);
        if (_packets.Count > MaxPackets)
            _packets.TrimFrontTo(MaxPackets);
    }

    public void Clear() => _packets.Clear();
}
