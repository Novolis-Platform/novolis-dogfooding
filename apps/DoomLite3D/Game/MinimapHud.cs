using System.Drawing;
using Novolis.Raylib.Game;

namespace DoomLite3D.Game;

internal static class MinimapHud
{
    private const int MapSize = 160;
    private const int Margin = 16;

    private static readonly Color PanelBg = Color.FromArgb(200, 12, 14, 18);
    private static readonly Color PanelBorder = Color.FromArgb(255, 60, 70, 90);
    private static readonly Color WallColor = Color.FromArgb(255, 55, 58, 68);
    private static readonly Color FloorColor = Color.FromArgb(255, 28, 30, 36);
    private static readonly Color PlayerColor = Color.FromArgb(255, 120, 220, 255);
    private static readonly Color EnemyColor = Color.FromArgb(255, 220, 70, 70);

    public static void Draw(
        RayGameContext ctx,
        LevelMap level,
        PlayerController player,
        EnemySystem enemies)
    {
        var originX = ctx.Width - MapSize - Margin;
        var originY = Margin;

        ctx.HudRect(originX - 2, originY - 2, MapSize + 4, MapSize + 4, PanelBorder);
        ctx.HudRect(originX, originY, MapSize, MapSize, PanelBg);

        var w = level.Walls.Width;
        var h = level.Walls.Height;
        var cellPx = (float)MapSize / Math.Max(w, h);
        var centerX = originX + MapSize / 2f;
        var centerY = originY + MapSize / 2f;

        var playerGx = player.Camera.Position.X / LevelMap.CellSize;
        var playerGz = player.Camera.Position.Z / LevelMap.CellSize;
        var cos = MathF.Cos(-player.Camera.Yaw);
        var sin = MathF.Sin(-player.Camera.Yaw);

        for (var z = 0u; z < h; z++)
        for (var x = 0u; x < w; x++)
        {
            var isWall = level.Walls.Get(x, z) != 0;
            var dx = x + 0.5f - playerGx;
            var dz = z + 0.5f - playerGz;
            var rx = dx * cos - dz * sin;
            var rz = dx * sin + dz * cos;
            var px = (int)(centerX + rx * cellPx);
            var py = (int)(centerY + rz * cellPx);
            var dot = Math.Max(1, (int)(cellPx * 0.85f));
            if (px < originX || py < originY || px >= originX + MapSize || py >= originY + MapSize)
                continue;
            ctx.HudRect(px, py, dot, dot, isWall ? WallColor : FloorColor);
        }

        foreach (var enemy in enemies.Enemies)
        {
            if (!enemy.Alive)
                continue;

            var ex = enemy.Position.X / LevelMap.CellSize;
            var ez = enemy.Position.Z / LevelMap.CellSize;
            var dx = ex - playerGx;
            var dz = ez - playerGz;
            var rx = dx * cos - dz * sin;
            var rz = dx * sin + dz * cos;
            var px = (int)(centerX + rx * cellPx) - 1;
            var py = (int)(centerY + rz * cellPx) - 1;
            if (px < originX || py < originY || px >= originX + MapSize - 2 || py >= originY + MapSize - 2)
                continue;
            ctx.HudRect(px, py, 3, 3, EnemyColor);
        }

        ctx.HudRect((int)centerX - 2, (int)centerY - 2, 4, 4, PlayerColor);
        ctx.HudText("MAP", originX + 6, originY + 4, 12, Color.FromArgb(255, 140, 150, 170));
    }
}
