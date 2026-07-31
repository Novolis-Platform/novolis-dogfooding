using System.Collections.Concurrent;
using Avalonia.Threading;
using Novolis.Transports.WireFish;

namespace WireFishViewer.Capture;

/// <summary>
/// Builds rows off the UI thread and flushes them in batches so a busy NIC cannot flood Avalonia.
/// </summary>
public sealed class UiPacketCaptureHandler(IPacketStore store) : IPacketHandler
{
    private readonly ConcurrentQueue<PacketRow> _pending = new();
    private int _sequence;
    private int _flushQueued;

    public bool CanHandle(DevicePacket packet) => true;

    public Task HandleAsync(DevicePacket packet, CancellationToken cancellationToken)
    {
        var number = Interlocked.Increment(ref _sequence);
        _pending.Enqueue(PacketRowFactory.FromDevicePacket(packet, number));
        ScheduleFlush();
        return Task.CompletedTask;
    }

    public void ResetSequence()
    {
        Interlocked.Exchange(ref _sequence, 0);
        while (_pending.TryDequeue(out _))
        {
            // drop in-flight rows from a previous session
        }

        Interlocked.Exchange(ref _flushQueued, 0);
    }

    private void ScheduleFlush()
    {
        if (Interlocked.CompareExchange(ref _flushQueued, 1, 0) != 0)
            return;

        Dispatcher.UIThread.Post(Flush, DispatcherPriority.Background);
    }

    private void Flush()
    {
        try
        {
            var batch = new List<PacketRow>(capacity: 256);
            while (_pending.TryDequeue(out var row))
                batch.Add(row);

            if (batch.Count > 0)
                store.AddRange(batch);
        }
        finally
        {
            Interlocked.Exchange(ref _flushQueued, 0);
            if (!_pending.IsEmpty)
                ScheduleFlush();
        }
    }
}
