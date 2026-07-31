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

    /// <summary>
    /// Starts capture. Must be called on the Avalonia UI thread (packet store is UI-bound).
    /// </summary>
    public async Task<CaptureStartOutcome> StartAsync(
        string? deviceCaptureKey,
        string? bpfFilter,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_captureHost is not null)
                return CaptureStartOutcome.AlreadyRunning();

            if (string.IsNullOrWhiteSpace(deviceCaptureKey))
                return CaptureStartOutcome.NoDeviceSelected();

            store.Clear();
            uiHandler.ResetSequence();

            _captureHost = Host.CreateDefaultBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddDebug();
                })
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
                            options.AllowNoCaptureDevices = false;
                            options.PromiscuousMode = true;
                        });
                })
                .Build();

            await _captureHost.StartAsync(cancellationToken);
            logger.LogInformation("WireFish capture session started on {Device}", deviceCaptureKey);
            return CaptureStartOutcome.Started();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start WireFish capture");
            await DisposeCaptureHostAsync();
            return CaptureStartOutcome.Failed(ex.GetBaseException().Message);
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

public readonly record struct CaptureStartOutcome(CaptureStartResult Result, string? Error = null)
{
    public static CaptureStartOutcome Started() => new(CaptureStartResult.Started);
    public static CaptureStartOutcome AlreadyRunning() => new(CaptureStartResult.AlreadyRunning);
    public static CaptureStartOutcome NoDeviceSelected() => new(CaptureStartResult.NoDeviceSelected);
    public static CaptureStartOutcome Failed(string error) => new(CaptureStartResult.Failed, error);
}
