using System.Drawing;
using System.Numerics;
using Novolis.Raylib.Game;
using Novolis.Raylib.Rendering;

namespace DoomLite3D.Game;

internal static class LevelRenderer
{
    private static readonly Color FloorColor = Color.FromArgb(255, 40, 36, 32);
    private static readonly Color WallColor = Color.FromArgb(255, 72, 68, 64);
    private static readonly Color WallTopColor = Color.FromArgb(255, 90, 82, 74);
    private static readonly Color CeilingColor = Color.FromArgb(255, 24, 22, 28);

    public static void Draw(RayGameContext ctx, Camera camera, LevelMap level)
    {
        var w = level.Walls.Width;
        var h = level.Walls.Height;
        var floorCenter = new Vector3(w * LevelMap.CellSize * 0.5f, 0f, h * LevelMap.CellSize * 0.5f);
        var floorSize = new Vector2(w * LevelMap.CellSize, h * LevelMap.CellSize);

        ctx.DrawPlane(floorCenter, floorSize, FloorColor);
        ctx.DrawPlane(floorCenter + new Vector3(0, LevelMap.WallHeight, 0), floorSize, CeilingColor);

        for (var z = 0u; z < h; z++)
        for (var x = 0u; x < w; x++)
        {
            if (level.Walls.Get(x, z) == 0)
                continue;

            var center = level.CellToWorld(x, z, LevelMap.WallHeight * 0.5f);
            var size = new Vector3(LevelMap.CellSize * 0.98f, LevelMap.WallHeight, LevelMap.CellSize * 0.98f);
            ctx.DrawShipBox(center, size, WallColor);
            ctx.DrawShipWires(center, size, WallTopColor);
        }
    }
}
