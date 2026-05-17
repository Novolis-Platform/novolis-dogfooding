using Novolis.Math.Arrays;
using Novolis.Physics.Collision.Simple;

namespace DoomLite3D.Game;

internal static class WallGridPhysics
{
    public static BvhStaticWorld BuildWorld(DenseGrid<byte> walls, float cellSize, float wallHeight)
    {
        var cells = new byte[walls.Width * walls.Height];
        for (var y = 0u; y < walls.Height; y++)
        for (var x = 0u; x < walls.Width; x++)
            cells[y * walls.Width + x] = walls.Get(x, y);

        return RoomMeshBuilder.FromWallGrid(walls.Width, walls.Height, cells, cellSize, wallHeight);
    }
}
