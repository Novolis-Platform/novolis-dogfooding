using System.Numerics;
using Novolis.Rendering.DependencyInjection;
using Novolis.Rendering.PathTrace.Demos;
using Novolis.Rendering.Presentation.Abstractions;
using Novolis.Rendering.Presentation.Silk;
using Novolis.Rendering.Runtime;
using Microsoft.Extensions.DependencyInjection;

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
    private int _sample;
    private CompiledScene? _scene;
    private string _status = "Starting renderer…";

    public PathTraceViewport()
    {
        var services = new ServiceCollection();
        services.AddRayTracing().UseCpuBackend();
        _backend = services.BuildServiceProvider().GetRequiredService<IRayTracingBackend>();
        _worker = new PathTraceBackgroundWorker(_backend, _display);
    }

    public SilkOrbitCamera Orbit => _orbit;

    public int DisplayedSamples => _display.DisplayedSampleCount;

    public bool IsReady => _width > 0 && _height > 0 && _scene is not null;

    public bool LastFramePresented { get; private set; }

    public string Status => _status;

    public void Attach(IFramePresenter presenter) => _presenter = presenter;

    public void TryResizeFromBounds(double width, double height)
    {
        var w = (int)Math.Max(0, width);
        var h = (int)Math.Max(0, height);
        if (w <= 0 || h <= 0)
        {
            _status = $"Waiting for viewport size ({w}×{h})…";
            return;
        }

        Resize(w, h);
    }

    public void Resize(int width, int height)
    {
        if (width <= 0 || height <= 0 || (width == _width && height == _height))
            return;

        _width = width;
        _height = height;
        _worker.WaitForIdle();
        _backend.ResizeAsync(width, height).GetAwaiter().GetResult();
        if (_scene is not null)
            _backend.UploadSceneAsync(_scene).GetAwaiter().GetResult();
        _display.Invalidate(width, height);
        _sample = 0;
        _status = $"Viewport {_width}×{_height} — tracing…";
    }

    public void SetScene(CompiledScene scene)
    {
        _scene = scene;
        if (_width > 0 && _height > 0)
        {
            _worker.WaitForIdle();
            _backend.UploadSceneAsync(scene).GetAwaiter().GetResult();
            _backend.ResetAccumulation();
            _display.Invalidate(_width, _height);
            _sample = 0;
            _status = $"Scene loaded — tracing at {_width}×{_height}";
        }
        else
        {
            _status = "Scene ready — waiting for viewport layout…";
        }
    }

    public void ResetAccumulation()
    {
        _worker.WaitForIdle();
        _backend.ResetAccumulation();
        if (_width > 0 && _height > 0)
            _display.Invalidate(_width, _height);
        _sample = 0;
    }

    public void Tick()
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
            _status = "Viewport has no size yet — resize the window";
            return;
        }

        if (_scene is null)
        {
            LastFramePresented = false;
            _status = "No scene — add a mesh";
            return;
        }

        var camera = BuildCamera();
        _worker.TryEnqueueAccumulate(camera, ref _sample, batchSize: 8);
        LastFramePresented = _display.TryPresent(_presenter);
        _status = LastFramePresented
            ? $"Rendering {_width}×{_height} — samples {_display.DisplayedSampleCount}"
            : $"Tracing… samples queued ({_sample})";
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
