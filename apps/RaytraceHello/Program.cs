using System.Numerics;
using Microsoft.Extensions.DependencyInjection;
using Novolis.Raylib.Game;
using Novolis.Raylib.Presentation;
using Novolis.Rendering.Backends.Cpu;
using Novolis.Rendering.DependencyInjection;
using Novolis.Rendering.Presentation.Abstractions;
using Novolis.Rendering.Runtime;

namespace RaytraceHello;

internal static class Program
{
    public static void Main()
    {
        var services = new ServiceCollection();
        services.AddRayTracing().UseCpuBackend();
        services.AddSingleton<IFramePresenter, RaylibCpuFramePresenter>();
        var provider = services.BuildServiceProvider();
        var backend = provider.GetRequiredService<IRayTracingBackend>();
        var presenter = provider.GetRequiredService<IFramePresenter>();
        var compiled = DemoSceneFactory.UnitCubeRoom();
        var sample = 0;

        RayGame.Run("RaytraceHello", 640, 480, ctx =>
        {
            if (sample == 0)
            {
                backend.ResizeAsync(ctx.Width, ctx.Height).GetAwaiter().GetResult();
                backend.UploadSceneAsync(compiled).GetAwaiter().GetResult();
            }

            var camera = CameraSnapshot.LookAt(
                new Vector3(2f, 1.2f, 3f),
                Vector3.Zero,
                Vector3.UnitY,
                60f,
                ctx.Width / (float)ctx.Height);

            backend.RenderAsync(camera, sample).GetAwaiter().GetResult();
            sample++;

            if (backend.Output.TryGetCpuPixels(out var pixels, out var w, out var h))
            {
                presenter.PresentCpuFrame(pixels, w, h);
            }

            ctx.Text($"Samples {backend.SampleCount}", 12, 12, 18, System.Drawing.Color.White);
        });
    }
}
