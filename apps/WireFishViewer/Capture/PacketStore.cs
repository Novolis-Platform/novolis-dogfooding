using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace WireFishViewer.Capture;

/// <summary>Observable list that supports bulk add with a single Reset notification.</summary>
internal sealed class PacketRowCollection : ObservableCollection<PacketRow>
{
    private bool _suppress;

    public void AddRange(IReadOnlyList<PacketRow> rows)
    {
        if (rows.Count == 0)
            return;

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
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public void TrimTo(int maxCount)
    {
        if (Count <= maxCount)
            return;

        _suppress = true;
        try
        {
            var remove = Count - maxCount;
            for (var i = 0; i < remove; i++)
                Items.RemoveAt(0);
        }
        finally
        {
            _suppress = false;
        }

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
            _packets.TrimTo(MaxPackets);
    }

    public void Clear() => _packets.Clear();
}
