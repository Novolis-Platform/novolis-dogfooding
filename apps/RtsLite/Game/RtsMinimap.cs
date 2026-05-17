using System.Drawing;
using System.Numerics;
using Novolis.Raylib.Game;

namespace RtsLite.Game;

internal static class RtsMinimap
{
    private const int Size = 140;
    private const int Margin = 16;

    public static void Draw(RayGameContext ctx, RtsArena arena, IReadOnlyList<RtsUnit> units)
    {
        var ox = ctx.Width - Size - Margin;
        var oy = Margin;
        ctx.HudRect(ox - 2, oy - 2, Size + 4, Size + 4, Color.FromArgb(255, 60, 70, 90));
        ctx.HudRect(ox, oy, Size, Size, Color.FromArgb(200, 18, 22, 28));

        var scale = (float)Size / RtsArena.GridSize;
        for (var z = 0u; z < arena.Walls.Height; z++)
        for (var x = 0u; x < arena.Walls.Width; x++)
        {
            var wall = arena.Walls[x, z, 0] != 0;
            var px = (int)(ox + x * scale);
            var py = (int)(oy + z * scale);
            var dot = Math.Max(1, (int)scale);
            ctx.HudRect(px, py, dot, dot, wall ? Color.FromArgb(255, 55, 58, 68) : Color.FromArgb(255, 32, 36, 44));
        }

        foreach (var unit in units)
        {
            var gx = unit.Position.X / RtsArena.CellSize;
            var gz = unit.Position.Z / RtsArena.CellSize;
            var px = (int)(ox + gx * scale);
            var py = (int)(oy + gz * scale);
            var color = unit.Team == UnitTeam.Player
                ? Color.FromArgb(255, 120, 220, 255)
                : Color.FromArgb(255, 220, 70, 70);
            ctx.HudRect(px, py, 3, 3, color);
        }

        ctx.HudText("MAP", ox + 6, oy + 4, 12, Color.FromArgb(255, 140, 150, 170));
    }
}
