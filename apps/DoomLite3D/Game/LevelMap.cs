using System.Numerics;
using Novolis.Math.Arrays;

namespace DoomLite3D.Game;

internal sealed class LevelMap
{
    public const float CellSize = 1f;
    public const float WallHeight = 3f;

    public DenseGrid<byte> Walls { get; }
    public IReadOnlyList<EnemySpawn> Enemies { get; }
    public GridIndex PlayerSpawn { get; }

    private LevelMap(DenseGrid<byte> walls, GridIndex playerSpawn, IReadOnlyList<EnemySpawn> enemies)
    {
        Walls = walls;
        PlayerSpawn = playerSpawn;
        Enemies = enemies;
    }

    public static LevelMap CreateRandom(int? seed = null)
    {
        var (walls, spawn, enemies) = MazeGenerator.Generate(seed);
        return new LevelMap(walls, spawn, enemies);
    }

    public Vector3 CellToWorld(uint x, uint z, float y = 0f) =>
        new((x + 0.5f) * CellSize, y, (z + 0.5f) * CellSize);
}

internal readonly record struct EnemySpawn(GridIndex Cell, int SpriteIndex);
