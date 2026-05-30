using System.Numerics;
using Microsoft.Extensions.DependencyInjection;
using Novolis.Rendering.DependencyInjection;
using Novolis.Rendering.PathTrace.Demos;
using Novolis.Rendering.Presentation.Abstractions;
using Novolis.Rendering.Presentation.Silk;
using Novolis.Rendering.Runtime;

namespace MeshBench.Services;

internal sealed class PathTraceViewport : IDisposable
{
    public const int MaxAccumulatedSamples = 96;

    private readonly IRayTracingBackend _backend;
    private readonly PathTraceDisplayBuffer _display = new();
    private readonly PathTraceBackgroundWorker _worker;
    private readonly SilkOrbitCamera _orbit = new() { Target = new Vector3(0f, 0.45f, 0f), Distance = 4.5f };
    private readonly object _resizeGate = new();

    private IFramePresenter? _presenter;
    private int _width;
    private int _height;
    private int _fullWidth;
    private int _fullHeight;
    private int _queuedSamples;
    private CompiledScene? _scene;
    private int _lastPresentedSampleCount = -1;
    private float _renderScale = 1f;
    private int _uploadGeneration;
    private int _resizeGeneration;
    private bool _paused = true;
    private string _status = "Preview mode";

    public PathTraceViewport()
    {
        var services = new ServiceCollection();
        services.AddRayTracingFromEnvironment();
        _backend = services.BuildServiceProvider().GetRequiredService<IRayTracingBackend>();
        _worker = new PathTraceBackgroundWorker(_backend, _display);
    }

    public string BackendLabel => _backend.BackendLabel;

    public SilkOrbitCamera Orbit => _orbit;

    public int DisplayedSamples => _display.DisplayedSampleCount;

    public bool IsReady => _width > 0 && _height > 0 && _scene is not null;

    public bool LastFramePresented { get; private set; }

    public string Status => _status;

    public bool IsPaused => _paused;

    public bool IsWorkerBusy => _worker.IsBusy;

    public void Attach(IFramePresenter presenter) => _presenter = presenter;

    public void BeginTracing()
    {
        _paused = false;
        _queuedSamples = 0;
        _lastPresentedSampleCount = -1;
        _status = $"{BackendLabel} — starting…";
    }

    public void StopTracing()
    {
        _paused = true;
        _queuedSamples = 0;
        _status = "Preview mode";
    }

    public void SetRenderScale(float scale) =>
        _renderScale = Math.Clamp(scale, 0.25f, 1f);

    public void TryResizeFromBounds(double width, double height)
    {
        var w = (int)Math.Max(0, width);
        var h = (int)Math.Max(0, height);
        if (w <= 0 || h <= 0)
        {
            _status = $"Waiting for viewport size ({w}×{h})…";
            return;
        }

        _fullWidth = Math.Min(w, 1920);
        _fullHeight = Math.Min(h, 1080);
        QueueResize();
    }

    private void QueueResize()
    {
        var w = Math.Max(64, (int)(_fullWidth * _renderScale));
        var h = Math.Max(64, (int)(_fullHeight * _renderScale));
        if (w == _width && h == _height)
            return;

        var generation = Interlocked.Increment(ref _resizeGeneration);
        _ = Task.Run(() => ApplyScaledSizeAsync(w, h, generation));
    }

    private async Task ApplyScaledSizeAsync(int w, int h, int generation)
    {
        await Task.Yield();
        if (generation != _resizeGeneration)
            return;

        await _worker.WaitForIdleAsync().ConfigureAwait(false);
        if (generation != _resizeGeneration)
            return;

        await _backend.ResizeAsync(w, h).ConfigureAwait(false);
        if (generation != _resizeGeneration)
            return;

        var scene = _scene;
        if (scene is not null)
            await _backend.UploadSceneAsync(scene).ConfigureAwait(false);

        if (generation != _resizeGeneration)
            return;

        lock (_resizeGate)
        {
            _width = w;
            _height = h;
            _display.Invalidate(w, h);
            _queuedSamples = 0;
            _lastPresentedSampleCount = -1;
        }

        _status = $"{BackendLabel} {_width}×{_height}";
    }

    public void SetScene(CompiledScene scene)
    {
        _scene = scene;
        _uploadGeneration++;
        var gen = _uploadGeneration;
        if (_width <= 0 || _height <= 0)
        {
            _status = "Scene ready — waiting for viewport layout…";
            return;
        }

        _ = Task.Run(() => UploadSceneAsync(scene, gen));
    }

    private async Task UploadSceneAsync(CompiledScene scene, int generation)
    {
        await Task.Yield();
        await _worker.WaitForIdleAsync().ConfigureAwait(false);
        if (generation != _uploadGeneration)
            return;

        await _backend.UploadSceneAsync(scene).ConfigureAwait(false);
        if (generation != _uploadGeneration)
            return;

        await _worker.WaitForIdleAsync().ConfigureAwait(false);
        if (generation != _uploadGeneration)
            return;

        _backend.ResetAccumulation();
        lock (_resizeGate)
        {
            if (_width > 0 && _height > 0)
                _display.Invalidate(_width, _height);
            _queuedSamples = 0;
            _lastPresentedSampleCount = -1;
        }

        _status = $"{BackendLabel} scene ready";
    }

    public void ResetAccumulation()
    {
        if (_paused || _width <= 0 || _height <= 0)
            return;

        _queuedSamples = 0;
        _lastPresentedSampleCount = -1;
        _ = Task.Run(async () =>
        {
            await _worker.WaitForIdleAsync().ConfigureAwait(false);
            _backend.ResetAccumulation();
            lock (_resizeGate)
            {
                _display.Invalidate(_width, _height);
            }
        });
    }

    public void Tick(int batchSize = 8)
    {
        if (_paused || _presenter is null)
        {
            LastFramePresented = false;
            return;
        }

        if (_width <= 0 || _height <= 0)
        {
            LastFramePresented = false;
            _status = "Waiting for viewport layout…";
            return;
        }

        if (_scene is null)
        {
            LastFramePresented = false;
            _status = "Compiling scene…";
            return;
        }

        TryPresentLatest();

        if (_queuedSamples >= MaxAccumulatedSamples)
        {
            _status = $"{BackendLabel} — {DisplayedSamples} samples";
            return;
        }

        if (_worker.IsBusy)
        {
            _status = $"{BackendLabel} tracing… ({DisplayedSamples} shown)";
            return;
        }

        var camera = BuildCamera();
        var batch = Math.Min(batchSize, MaxAccumulatedSamples - _queuedSamples);
        if (batch > 0 && _worker.TryEnqueueAccumulate(camera, ref _queuedSamples, batch))
            _status = $"{BackendLabel} tracing… ({DisplayedSamples} shown)";

        TryPresentLatest();
    }

    private void TryPresentLatest()
    {
        if (_presenter is null || _worker.IsBusy)
            return;

        var shown = DisplayedSamples;
        if (shown <= 0 || shown == _lastPresentedSampleCount)
            return;

        if (_display.TryPresent(_presenter))
        {
            LastFramePresented = true;
            _lastPresentedSampleCount = shown;
        }
    }

    public CameraSnapshot BuildCamera()
    {
        var eye = _orbit.BuildEyePosition();
        return CameraSnapshot.LookAt(
            eye,
            _orbit.Target,
            Vector3.UnitY,
            _orbit.FieldOfViewDegrees,
            _width / (float)Math.Max(1, _height));
    }

    public void ApplyCameraState(Models.OrbitCameraState state)
    {
        _orbit.Yaw = state.Yaw;
        _orbit.Pitch = state.Pitch;
        _orbit.Distance = state.Distance;
        if (state.Target.Length >= 3)
            _orbit.Target = new Vector3(state.Target[0], state.Target[1], state.Target[2]);
    }

    public Models.OrbitCameraState CaptureCameraState() =>
        new()
        {
            Yaw = _orbit.Yaw,
            Pitch = _orbit.Pitch,
            Distance = _orbit.Distance,
            Target = [_orbit.Target.X, _orbit.Target.Y, _orbit.Target.Z],
        };

    public void FitToBounds(Vector3 center, float radius)
    {
        _orbit.Target = center;
        _orbit.Distance = MathF.Max(2f, radius * 2.8f);
        ResetAccumulation();
    }

    public void Dispose()
    {
        _worker.Dispose();
        if (_backend is IDisposable d)
            d.Dispose();
    }
}

internal static class PathTraceWorkerExtensions
{
    public static Task WaitForIdleAsync(this PathTraceBackgroundWorker worker) =>
        Task.Run(worker.WaitForIdle);
}
