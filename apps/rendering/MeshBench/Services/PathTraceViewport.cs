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
    private readonly IRayTracingBackend _backend;
    private readonly PathTraceDisplayBuffer _display = new();
    private readonly PathTraceBackgroundWorker _worker;
    private readonly SilkOrbitCamera _orbit = new() { Target = new Vector3(0f, 0.45f, 0f), Distance = 4.5f };

    private IFramePresenter? _presenter;
    private int _width;
    private int _height;
    private int _fullWidth;
    private int _fullHeight;
    private int _sample;
    private CompiledScene? _scene;
    private int _lastPresentedGeneration = -1;
    private float _renderScale = 1f;
    private int _uploadGeneration;
    private string _status = "Starting renderer…";

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

    public void Attach(IFramePresenter presenter) => _presenter = presenter;

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
        ApplyScaledSize();
    }

    private void ApplyScaledSize()
    {
        var w = Math.Max(64, (int)(_fullWidth * _renderScale));
        var h = Math.Max(64, (int)(_fullHeight * _renderScale));
        if (w == _width && h == _height)
            return;

        _width = w;
        _height = h;
        _worker.WaitForIdle();
        _backend.ResizeAsync(w, h).GetAwaiter().GetResult();
        if (_scene is not null)
            _backend.UploadSceneAsync(_scene).GetAwaiter().GetResult();
        _display.Invalidate(w, h);
        _sample = 0;
        _lastPresentedGeneration = -1;
        _status = $"{BackendLabel} {_width}×{_height} (scale {(int)(_renderScale * 100)}%) — tracing…";
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

        Task.Run(() => UploadSceneAsync(scene, gen));
    }

    private async Task UploadSceneAsync(CompiledScene scene, int generation)
    {
        await Task.Yield();
        _worker.WaitForIdle();
        if (generation != _uploadGeneration)
            return;

        await _backend.UploadSceneAsync(scene).ConfigureAwait(false);
        if (generation != _uploadGeneration)
            return;

        _worker.WaitForIdle();
        _backend.ResetAccumulation();
        _display.Invalidate(_width, _height);
        _sample = 0;
        _lastPresentedGeneration = -1;
        _status = $"{BackendLabel} scene loaded — tracing at {_width}×{_height}";
    }

    public void ResetAccumulation()
    {
        _worker.WaitForIdle();
        _backend.ResetAccumulation();
        if (_width > 0 && _height > 0)
            _display.Invalidate(_width, _height);
        _sample = 0;
        _lastPresentedGeneration = -1;
    }

    public void Tick(int batchSize = 8)
    {
        if (_presenter is null)
        {
            LastFramePresented = false;
            _status = "No viewport attached";
            return;
        }

        if (_width <= 0 || _height <= 0)
        {
            LastFramePresented = false;
            _status = "Viewport has no size yet";
            return;
        }

        if (_scene is null)
        {
            LastFramePresented = false;
            _status = "No scene — add a mesh";
            return;
        }

        var camera = BuildCamera();
        _worker.TryEnqueueAccumulate(camera, ref _sample, batchSize);
        var samples = _display.DisplayedSampleCount;
        if (samples == _lastPresentedGeneration)
        {
            LastFramePresented = false;
        }
        else
        {
            LastFramePresented = _display.TryPresent(_presenter);
            if (LastFramePresented)
                _lastPresentedGeneration = samples;
        }
        _status = LastFramePresented
            ? $"{BackendLabel} {_width}×{_height} — samples {_display.DisplayedSampleCount}"
            : $"{BackendLabel} tracing… ({_sample} samples queued)";
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
