using System.Diagnostics;
using System.Drawing;
using Novolis.Raylib.Game;

namespace BouncingBall.Game;

internal static class DiagnosticsHud
{
    public static bool Visible { get; private set; } = true;

    private static float _smoothedFps;
    private static bool _fpsInitialized;

    public static void Toggle() => Visible = !Visible;

    public static void Draw(RayGameContext ctx, BallWorld world)
    {
        if (!Visible)
            return;

        var dt = ctx.DeltaSeconds;
        if (dt > 1e-6f)
        {
            var instantFps = 1f / dt;
            _smoothedFps = _fpsInitialized ? _smoothedFps * 0.9f + instantFps * 0.1f : instantFps;
            _fpsInitialized = true;
        }

        var grounded = 0;
        var speedSum = 0f;
        var speedMax = 0f;
        foreach (var ball in world.Balls)
        {
            if (ball.IsGrounded)
                grounded++;
            speedSum += ball.Speed;
            speedMax = MathF.Max(speedMax, ball.Speed);
        }

        var avgSpeed = world.BallCount > 0 ? speedSum / world.BallCount : 0f;

        const int x = 16;
        const int y = 52;
        const int w = 340;
        const int lineH = 17;
        const int lines = 13;
        var h = lineH * lines + 12;

        ctx.HudRect(x - 4, y - 4, w + 8, h + 8, Color.FromArgb(220, 8, 10, 14));
        ctx.HudRect(x, y, w, h, Color.FromArgb(200, 20, 24, 32));

        var proc = Process.GetCurrentProcess();
        var workingMb = proc.WorkingSet64 / (1024.0 * 1024.0);
        var gcMb = GC.GetTotalMemory(false) / (1024.0 * 1024.0);
        var frameMs = dt * 1000f;
        var text = Color.FromArgb(255, 180, 220, 200);
        var accent = Color.FromArgb(255, 120, 200, 255);
        var dim = Color.FromArgb(255, 120, 130, 150);

        var row = y + 6;
        ctx.HudText("DIAGNOSTICS", x + 8, row, 14, accent);
        row += lineH;
        ctx.HudText($"FPS {_smoothedFps:F0}  ({frameMs:F1} ms)", x + 8, row, 14, text);
        row += lineH;
        ctx.HudText(
            $"Balls {world.BallCount}  active {world.ActiveBallCount}  sleep {world.SleepingBallCount}",
            x + 8,
            row,
            14,
            text);
        row += lineH;
        ctx.HudText($"Grounded {grounded}  speed avg {avgSpeed:F2}  max {speedMax:F2}", x + 8, row, 14, text);
        row += lineH;
        ctx.HudText(
            $"Ball-ball hits {world.BallBallContactsLastFrame}  checks {world.BallBallPairChecksLastFrame}",
            x + 8,
            row,
            14,
            text);
        row += lineH;
        var skipLabel = world.BallBallSkippedLastFrame ? "yes" : "no";
        ctx.HudText($"Ball-ball skip (pile settled) {skipLabel}", x + 8, row, 14, text);
        row += lineH;
        ctx.HudText(
            $"Phys substeps {world.PhysicsSubStepsLastFrame}  solve iters {world.BallBallSolveIterationsLastFrame}",
            x + 8,
            row,
            14,
            text);
        row += lineH;
        ctx.HudText(
            $"Wall refl {world.IntegratorReflectionsLastFrame}  clamped {world.ClampedBallsLastFrame}",
            x + 8,
            row,
            14,
            text);
        row += lineH;
        ctx.HudText($"RAM {workingMb:F0} MB  GC {gcMb:F1} MB", x + 8, row, 14, text);
        row += lineH;
        ctx.HudText("B / Ctrl+B / Ctrl+Shift+B  R  F3", x + 8, row, 12, dim);
    }
}
