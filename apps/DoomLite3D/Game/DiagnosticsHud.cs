using System.Diagnostics;
using System.Drawing;
using Novolis.Raylib.Game;

namespace DoomLite3D.Game;

internal static class DiagnosticsHud
{
    public static bool Visible { get; private set; }

    private static float _smoothedFps;
    private static bool _initialized;

    public static void Toggle() => Visible = !Visible;

    public static void Draw(
        RayGameContext ctx,
        LevelMap level,
        EnemySystem enemies)
    {
        if (!Visible)
            return;

        var dt = ctx.DeltaSeconds;
        if (dt > 1e-6f)
        {
            var instantFps = 1f / dt;
            _smoothedFps = _initialized ? _smoothedFps * 0.9f + instantFps * 0.1f : instantFps;
            _initialized = true;
        }

        const int x = 16;
        const int y = 84;
        const int w = 220;
        const int lineH = 18;
        var lines = 7;
        var h = lineH * lines + 12;

        ctx.HudRect(x - 4, y - 4, w + 8, h + 8, Color.FromArgb(220, 8, 10, 14));
        ctx.HudRect(x, y, w, h, Color.FromArgb(200, 20, 24, 32));

        var proc = Process.GetCurrentProcess();
        var workingMb = proc.WorkingSet64 / (1024.0 * 1024.0);
        var gcMb = GC.GetTotalMemory(false) / (1024.0 * 1024.0);
        var total = enemies.CountTotal();
        var alive = enemies.CountAlive();
        var frameMs = dt * 1000f;
        var textColor = Color.FromArgb(255, 180, 220, 200);
        var accent = Color.FromArgb(255, 120, 200, 255);

        var row = y + 6;
        ctx.HudText("DIAGNOSTICS", x + 8, row, 14, accent);
        row += lineH;
        ctx.HudText($"FPS {_smoothedFps:F0}  ({frameMs:F1} ms)", x + 8, row, 14, textColor);
        row += lineH;
        ctx.HudText($"RAM {workingMb:F0} MB", x + 8, row, 14, textColor);
        row += lineH;
        ctx.HudText($"GC heap {gcMb:F1} MB", x + 8, row, 14, textColor);
        row += lineH;
        ctx.HudText($"Enemies {alive}/{total}", x + 8, row, 14, textColor);
        row += lineH;
        ctx.HudText($"Seed {level.Seed}", x + 8, row, 14, textColor);
        row += lineH;
        ctx.HudText("F3 hide", x + 8, row, 12, Color.FromArgb(255, 120, 130, 150));
    }
}
