using Avalonia.Threading;
using Novolis.Transports.WireFish;

namespace WireFishViewer.Capture;

public sealed class UiPacketCaptureHandler(IPacketStore store) : IPacketHandler
{
    private int _sequence;

    public bool CanHandle(DevicePacket packet) => true;

    public Task HandleAsync(DevicePacket packet, CancellationToken cancellationToken)
    {
        var number = Interlocked.Increment(ref _sequence);
        var row = PacketRowFactory.FromDevicePacket(packet, number);
        Dispatcher.UIThread.Post(() => store.Add(row));
        return Task.CompletedTask;
    }

    public void ResetSequence() => Interlocked.Exchange(ref _sequence, 0);
}
