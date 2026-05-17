using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Novolis.Transports.WireFish;

namespace WireFishViewer.Capture;

public sealed class CaptureSessionService(
    ILogger<CaptureSessionService> logger,
    IPacketStore store,
    UiPacketCaptureHandler uiHandler) : IAsyncDisposable
{
    private IHost? _captureHost;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public bool IsCapturing => _captureHost is not null;

    public async Task<CaptureStartResult> StartAsync(string? deviceCaptureKey, string? bpfFilter, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_captureHost is not null)
                return CaptureStartResult.AlreadyRunning;

            if (string.IsNullOrWhiteSpace(deviceCaptureKey))
                return CaptureStartResult.NoDeviceSelected;

            store.Clear();
            uiHandler.ResetSequence();

            _captureHost = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton(store);
                    services.AddSingleton(uiHandler);
                    services.AddNovolisWireFish(
                        builder => builder.AddPacketHandler<UiPacketCaptureHandler>(),
                        options =>
                        {
                            options.CaptureAllDevices = false;
                            options.DeviceNames.Add(deviceCaptureKey);
                            options.BpfFilter = string.IsNullOrWhiteSpace(bpfFilter) ? null : bpfFilter.Trim();
                            options.AllowNoCaptureDevices = true;
                            options.PromiscuousMode = true;
                        });
                })
                .Build();

            await _captureHost.StartAsync(cancellationToken);
            logger.LogInformation("WireFish capture session started on {Device}", deviceCaptureKey);
            return CaptureStartResult.Started;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start WireFish capture");
            await DisposeCaptureHostAsync();
            return CaptureStartResult.Failed;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await DisposeCaptureHostAsync();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync();
        try
        {
            await DisposeCaptureHostAsync();
        }
        finally
        {
            _gate.Release();
            _gate.Dispose();
        }
    }

    private async Task DisposeCaptureHostAsync()
    {
        if (_captureHost is null)
            return;

        try
        {
            await _captureHost.StopAsync();
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Capture host stop reported an error");
        }

        _captureHost.Dispose();
        _captureHost = null;
    }
}

public enum CaptureStartResult
{
    Started,
    AlreadyRunning,
    NoDeviceSelected,
    Failed,
}
