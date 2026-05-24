using System.Numerics;
using Microsoft.Extensions.DependencyInjection;
using Novolis.Math.Geometry;
using Novolis.Raylib.Game;
using Novolis.Raylib.Interact;
using Novolis.Raylib.Presentation;
using Novolis.Rendering.Compile;
using Novolis.Rendering.DependencyInjection;
using Novolis.Rendering.Materials;
using Novolis.Rendering.Presentation.Abstractions;
using Novolis.Rendering.Runtime;
using Novolis.Rendering.Scene;

namespace RaytraceHello;

internal static class Program
{
    private const int OrbitSamplesPerFrame = 4;
    private const int AccumulateSamplesPerBatch = 3;

    private static readonly Vector3 CubeCenter = new(0f, 0.25f, 0f);
    private static readonly Vector3 CubeHalfExtents = new(0.25f, 0.25f, 0.25f);
    private static readonly Vector3 DefaultEye = new(1.2f, 0.8f, 2f);

    public static void Main()
    {
        var services = new ServiceCollection();
        services.AddRayTracing().UseIlgpuBackend();
        services.AddSingleton<RaylibCpuFramePresenter>();
        services.AddSingleton<IFramePresenter>(sp =>
            new YFlippedFramePresenter(sp.GetRequiredService<RaylibCpuFramePresenter>()));
        var provider = services.BuildServiceProvider();
        var backend = provider.GetRequiredService<IRayTracingBackend>();
        var presenter = provider.GetRequiredService<IFramePresenter>();
        var compiled = BuildShowcaseScene();
        var frame = new FrameSnapshot();
        var worker = new BackgroundTracer(backend, frame);
        var sample = 0;
        var frameWidth = 0;
        var frameHeight = 0;
        var orbitAngle = 0f;
        var orbitEnabled = false;

        RayGame.Run("RaytraceHello — ILGPU path tracing", 960, 540, ctx =>
        {
            if (ctx.Width != frameWidth || ctx.Height != frameHeight)
            {
                frameWidth = ctx.Width;
                frameHeight = ctx.Height;
                worker.WaitForIdle();
                backend.ResizeAsync(frameWidth, frameHeight).GetAwaiter().GetResult();
                backend.UploadSceneAsync(compiled).GetAwaiter().GetResult();
                frame.Invalidate(frameWidth, frameHeight);
                sample = 0;
            }

            if (ctx.IsKeyPressed(KeyboardKey.R))
            {
                worker.WaitForIdle();
                backend.ResetAccumulation();
                frame.Invalidate(frameWidth, frameHeight);
                sample = 0;
            }

            if (ctx.IsKeyPressed(KeyboardKey.Space))
            {
                orbitEnabled = !orbitEnabled;
                worker.WaitForIdle();
                backend.ResetAccumulation();
                frame.Invalidate(frameWidth, frameHeight);
                sample = 0;
            }

            var eye = orbitEnabled
                ? CubeCenter + new Vector3(
                    MathF.Sin(orbitAngle) * 2.4f,
                    0.85f,
                    MathF.Cos(orbitAngle) * 2.4f)
                : DefaultEye;

            if (orbitEnabled)
            {
                orbitAngle += ctx.DeltaSeconds * 0.35f;
            }

            var camera = CameraSnapshot.LookAt(
                eye,
                CubeCenter,
                Vector3.UnitY,
                52f,
                frameWidth / (float)frameHeight);

            if (orbitEnabled)
            {
                worker.EnqueueOrbit(camera, OrbitSamplesPerFrame);
            }
            else
            {
                worker.TryEnqueueAccumulate(camera, ref sample, AccumulateSamplesPerBatch);
            }

            frame.TryPresent(presenter);
            DrawHud(ctx, backend, backend.SampleCount, orbitEnabled);
        });

        worker.Dispose();
        if (backend is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private static string ResolveBackendLabel(IRayTracingBackend backend) =>
        backend.GetType().Name switch
        {
            "IlgpuRayTracingBackend" => "ILGPU path tracing",
            "VulkanRayTracingBackend" => "Vulkan path tracing",
            _ => "CPU path tracing",
        };

    private static CompiledScene BuildShowcaseScene()
    {
        var scene = new SceneBuilder()
            .AddGround(MaterialPresets.Standard(new Vector3(0.38f, 0.4f, 0.44f), 0.85f))
            .AddBox(
                CubeCenter,
                CubeHalfExtents,
                MaterialPresets.Standard(MaterialPresets.Colors.Red, roughness: 0.35f, metallic: 0.15f))
            .AddBox(
                new Vector3(-0.45f, 0.32f, 0.12f),
                new Vector3(0.3f, 0.32f, 0.025f),
                MaterialPresets.Metal(MaterialPresets.Colors.Silver, roughness: 0.03f))
            .AddDirectionalLight(new Vector3(-0.35f, -1f, -0.25f), new Vector3(1f, 0.98f, 0.95f), 1.1f)
            .AddDirectionalLight(new Vector3(0.6f, -0.4f, 0.5f), new Vector3(0.55f, 0.65f, 0.85f), 0.45f)
            .Build();
        return SceneCompiler.Compile(scene);
    }

    private static void DrawHud(RayGameContext ctx, IRayTracingBackend backend, int sampleCount, bool orbitEnabled)
    {
        const int pad = 12;
        var line = 22;
        var y = pad;
        ctx.Rect(0, 0, 420, 88, System.Drawing.Color.FromArgb(160, 0, 0, 0));
        ctx.Text(ResolveBackendLabel(backend), pad, y, 20, System.Drawing.Color.White);
        y += line;
        ctx.Text($"Samples {sampleCount}", pad, y, 18, System.Drawing.Color.LightGray);
        y += line;
        var orbitLabel = orbitEnabled
            ? $"on ({OrbitSamplesPerFrame} spp/frame, async)"
            : "off (accumulating, async)";
        ctx.Text($"Space orbit {orbitLabel} · R reset", pad, y, 16, System.Drawing.Color.Gray);
    }

    /// <summary>Raylib textures are bottom-up; flip rows until Presentation package includes this.</summary>
    private sealed class YFlippedFramePresenter(IFramePresenter inner) : IFramePresenter
    {
        private Rgba32[]? _scratch;

        public void PresentCpuFrame(ReadOnlySpan<Rgba32> pixels, int width, int height)
        {
            var count = width * height;
            if (_scratch is null || _scratch.Length != count)
            {
                _scratch = new Rgba32[count];
            }

            for (var y = 0; y < height; y++)
            {
                pixels.Slice(y * width, width).CopyTo(_scratch.AsSpan((height - 1 - y) * width, width));
            }

            inner.PresentCpuFrame(_scratch, width, height);
        }
    }

    private sealed class FrameSnapshot
    {
        private readonly object _gate = new();
        private Rgba32[]? _pixels;
        private int _width;
        private int _height;

        public void Invalidate(int width, int height)
        {
            lock (_gate)
            {
                var count = width * height;
                if (_pixels is null || _pixels.Length != count)
                {
                    _pixels = new Rgba32[count];
                }

                _width = width;
                _height = height;
                Array.Clear(_pixels);
            }
        }

        public void Publish(ReadOnlySpan<Rgba32> source, int width, int height)
        {
            lock (_gate)
            {
                var count = width * height;
                if (_pixels is null || _pixels.Length != count)
                {
                    _pixels = new Rgba32[count];
                }

                source.CopyTo(_pixels);
                _width = width;
                _height = height;
            }
        }

        public bool TryPresent(IFramePresenter presenter)
        {
            lock (_gate)
            {
                if (_pixels is null || _width <= 0 || _height <= 0)
                {
                    return false;
                }

                presenter.PresentCpuFrame(_pixels, _width, _height);
                return true;
            }
        }
    }

    private sealed class BackgroundTracer(IRayTracingBackend backend, FrameSnapshot frame) : IDisposable
    {
        private readonly object _gate = new();
        private Task? _task;
        private bool _disposed;

        public void EnqueueOrbit(CameraSnapshot camera, int samplesPerFrame)
        {
            if (_disposed)
            {
                return;
            }

            lock (_gate)
            {
                if (_task is { IsCompleted: false })
                {
                    return;
                }

                _task = Task.Run(() =>
                {
                    backend.ResetAccumulation();
                    for (var s = 0; s < samplesPerFrame; s++)
                    {
                        backend.RenderAsync(camera, s).GetAwaiter().GetResult();
                    }

                    PublishFrame();
                });
            }
        }

        public bool TryEnqueueAccumulate(CameraSnapshot camera, ref int sample, int batchSize)
        {
            if (_disposed)
            {
                return false;
            }

            lock (_gate)
            {
                if (_task is { IsCompleted: false })
                {
                    return false;
                }

                var start = sample;
                _task = Task.Run(() =>
                {
                    for (var i = 0; i < batchSize; i++)
                    {
                        backend.RenderAsync(camera, start + i).GetAwaiter().GetResult();
                    }

                    PublishFrame();
                });
                sample = start + batchSize;
                return true;
            }
        }

        public void WaitForIdle()
        {
            Task? task;
            lock (_gate)
            {
                task = _task;
            }

            task?.GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            WaitForIdle();
        }

        private void PublishFrame()
        {
            if (!backend.Output.TryGetCpuPixels(out var pixels, out var w, out var h))
            {
                return;
            }

            frame.Publish(pixels, w, h);
        }
    }
}
