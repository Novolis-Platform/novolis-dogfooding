using System.Numerics;
using Novolis.Avalonia.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Novolis.Rendering.DependencyInjection;
using Novolis.Rendering.PathTrace.Demos;
using Novolis.Rendering.Presentation.Silk;
using Novolis.Rendering.Runtime;

namespace MeshBench.Services;

internal sealed class PathTraceViewport : IDisposable
{
    private readonly IRayTracingBackend _backend;
    private readonly PathTraceDisplayBuffer _display = new();
    private readonly PathTraceBackgroundWorker _worker;
    private readonly SilkOrbitCamera _orbit = new() { Target = new Vector3(0f, 0.45f, 0f), Distance = 4.5f };
    private Rgba32FrameControl? _control;
    private int _width;
    private int _height;
    private int _sample;
    private CompiledScene? _scene;

    public PathTraceViewport()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddRayTracing().UseCpuBackend();
        _backend = services.BuildServiceProvider().GetRequiredService<IRayTracingBackend>();
        _worker = new PathTraceBackgroundWorker(_backend, _display);
    }

    public SilkOrbitCamera Orbit => _orbit;

    public int DisplayedSamples => _display.DisplayedSampleCount;

    public void Attach(Rgba32FrameControl control) => _control = control;

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
        if (_control is null || _width <= 0 || _height <= 0 || _scene is null)
            return;

        var camera = BuildCamera();
        _worker.TryEnqueueAccumulate(camera, ref _sample, batchSize: 2);
        if (_control is not null)
            _display.TryPresent(_control);
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
