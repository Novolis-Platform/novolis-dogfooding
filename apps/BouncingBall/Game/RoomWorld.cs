using Novolis.Math.Arrays;
using Novolis.Physics.Collision.Simple;
using Novolis.Physics.Numerics;
using Novolis.Simulation.World.Builders;

namespace BouncingBall.Game;

/// <summary>Occupancy grid room: wall columns via simulation builder, floor/ceiling caps at app layer.</summary>
internal sealed class RoomWorld
{
    public const uint GridSize = 12;
    public const float CellSize = 1f;
    public const float WallHeight = 4f;

    public DenseGrid<byte> Walls { get; }

    /// <summary>Wall columns from <see cref="OccupancyColumnMeshBuilder"/> (simulation dogfood).</summary>
    public BvhStaticWorld SimulationWallColumns { get; }

    /// <summary>Enclosed collision volume (walls + floor + ceiling).</summary>
    public BvhStaticWorld CollisionWorld { get; }

    public Vector3d RoomCenter { get; }

    private RoomWorld(DenseGrid<byte> walls, BvhStaticWorld simulationWalls, BvhStaticWorld collisionWorld, Vector3d roomCenter)
    {
        Walls = walls;
        SimulationWallColumns = simulationWalls;
        CollisionWorld = collisionWorld;
        RoomCenter = roomCenter;
    }

    public static RoomWorld Create()
    {
        var walls = BuildPerimeterGrid();
        var cells = ToCellSpan(walls);
        var simulationWalls = OccupancyColumnMeshBuilder.FromWallGrid(
            walls.Width,
            walls.Height,
            cells,
            CellSize,
            WallHeight);
        var collisionWorld = BuildEnclosedRoom(walls, cells);
        var roomCenter = new Vector3d(GridSize * CellSize * 0.5, WallHeight * 0.5, GridSize * CellSize * 0.5);
        return new RoomWorld(walls, simulationWalls, collisionWorld, roomCenter);
    }

    private static DenseGrid<byte> BuildPerimeterGrid()
    {
        var grid = new DenseGrid<byte>(GridSize, GridSize);
        for (var y = 0u; y < GridSize; y++)
        for (var x = 0u; x < GridSize; x++)
        {
            var border = x == 0 || y == 0 || x == GridSize - 1 || y == GridSize - 1;
            grid[x, y, 0] = border ? (byte)1 : (byte)0;
        }

        return grid;
    }

    private static BvhStaticWorld BuildEnclosedRoom(DenseGrid<byte> walls, ReadOnlySpan<byte> cells)
    {
        var verts = new List<Vector3d>();
        var tris = new List<int>();

        for (var y = 0u; y < walls.Height; y++)
        for (var x = 0u; x < walls.Width; x++)
        {
            var index = (int)(y * walls.Width + x);
            if (index >= cells.Length || cells[index] == 0)
                continue;

            var cx = (x + 0.5) * CellSize;
            var cz = (y + 0.5) * CellSize;
            var h = WallHeight * 0.5;
            var hx = CellSize * 0.5;
            AppendBox(verts, tris, cx, h, cz, hx, h, hx);
        }

        var x0 = CellSize;
        var x1 = (GridSize - 1) * CellSize;
        var z0 = CellSize;
        var z1 = (GridSize - 1) * CellSize;
        AppendQuad(verts, tris, new(x0, 0, z0), new(x1, 0, z0), new(x1, 0, z1), new(x0, 0, z1));
        AppendQuad(verts, tris, new(x0, WallHeight, z1), new(x1, WallHeight, z1), new(x1, WallHeight, z0), new(x0, WallHeight, z0));

        return new BvhStaticWorld(new StaticTriangleMesh(verts.ToArray(), tris.ToArray()));
    }

    private static ReadOnlySpan<byte> ToCellSpan(DenseGrid<byte> walls)
    {
        var cells = new byte[walls.Width * walls.Height];
        for (var y = 0u; y < walls.Height; y++)
        for (var x = 0u; x < walls.Width; x++)
            cells[y * walls.Width + x] = walls[x, y, 0];
        return cells;
    }

    private static void AppendBox(
        List<Vector3d> verts,
        List<int> tris,
        double cx,
        double cy,
        double cz,
        double hx,
        double hy,
        double hz)
    {
        var x0 = cx - hx;
        var x1 = cx + hx;
        var y0 = cy - hy;
        var y1 = cy + hy;
        var z0 = cz - hz;
        var z1 = cz + hz;

        AppendQuad(verts, tris, new(x0, y0, z0), new(x1, y0, z0), new(x1, y1, z0), new(x0, y1, z0));
        AppendQuad(verts, tris, new(x1, y0, z1), new(x0, y0, z1), new(x0, y1, z1), new(x1, y1, z1));
        AppendQuad(verts, tris, new(x0, y0, z0), new(x0, y0, z1), new(x0, y1, z1), new(x0, y1, z0));
        AppendQuad(verts, tris, new(x1, y0, z1), new(x1, y0, z0), new(x1, y1, z0), new(x1, y1, z1));
        AppendQuad(verts, tris, new(x0, y0, z1), new(x1, y0, z1), new(x1, y0, z0), new(x0, y0, z0));
        AppendQuad(verts, tris, new(x0, y1, z0), new(x1, y1, z0), new(x1, y1, z1), new(x0, y1, z1));
    }

    private static void AppendQuad(List<Vector3d> verts, List<int> tris, Vector3d a, Vector3d b, Vector3d c, Vector3d d)
    {
        var o = verts.Count;
        verts.Add(a);
        verts.Add(b);
        verts.Add(c);
        verts.Add(d);
        tris.Add(o);
        tris.Add(o + 1);
        tris.Add(o + 2);
        tris.Add(o);
        tris.Add(o + 2);
        tris.Add(o + 3);
    }
}
