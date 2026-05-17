using System.Numerics;
using Novolis.Math.Arrays;

namespace RtsLite.Game;

internal sealed class RtsArena
{
    public const uint GridSize = 32;
    public const float CellSize = 1f;

    public DenseGrid<byte> Walls { get; }

    public Vector3 SpawnPlayer => CellCenter(4, 4);
    public Vector3 SpawnEnemy => CellCenter(GridSize - 5, GridSize - 5);

    private RtsArena(DenseGrid<byte> walls) => Walls = walls;

    public static RtsArena Create()
    {
        var grid = new DenseGrid<byte>(GridSize, GridSize);
        for (var z = 0u; z < GridSize; z++)
        for (var x = 0u; x < GridSize; x++)
        {
            var border = x == 0 || z == 0 || x == GridSize - 1 || z == GridSize - 1;
            var pond = x is > 12 and < 20 && z is > 12 and < 20;
            grid[x, z, 0] = border || pond ? (byte)1 : (byte)0;
        }

        return new RtsArena(grid);
    }

    public Vector3 CellCenter(uint gx, uint gz) =>
        new((gx + 0.5f) * CellSize, 0f, (gz + 0.5f) * CellSize);

    public bool IsBlocked(float x, float z)
    {
        var gx = (int)(x / CellSize);
        var gz = (int)(z / CellSize);
        if (gx < 0 || gz < 0 || gx >= Walls.Width || gz >= Walls.Height)
            return true;
        return Walls[(uint)gx, (uint)gz, 0] != 0;
    }
}
