using System.Numerics;
using Novolis.Math.Arrays;

namespace DoomLite3D.Game;

internal sealed class LevelMap
{
    public const float CellSize = 1f;
    public const float WallHeight = 2f;

    public DenseGrid<byte> Walls { get; }
    public IReadOnlyList<EnemySpawn> Enemies { get; }
    public GridIndex PlayerSpawn { get; }

    private LevelMap(DenseGrid<byte> walls, GridIndex playerSpawn, IReadOnlyList<EnemySpawn> enemies)
    {
        Walls = walls;
        PlayerSpawn = playerSpawn;
        Enemies = enemies;
    }

    public static LevelMap CreateDefault()
    {
        const string layout = """
            ####################
            #..................#
            #..@...............#
            #..................#
            #......e...........#
            #.........e....e...#
            #..................#
            #..................#
            ####################
            """;

        var lines = layout
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var height = (uint)lines.Length;
        var width = (uint)lines[0].Length;
        var walls = new DenseGrid<byte>(width, height);
        var enemies = new List<EnemySpawn>();
        var playerSpawn = new GridIndex(2, 2);

        for (var z = 0u; z < height; z++)
        {
            var row = lines[z];
            for (var x = 0u; x < width; x++)
            {
                var ch = row[(int)x];
                switch (ch)
                {
                    case '#':
                        walls.Set(new GridIndex(x, z), 1);
                        break;
                    case '@':
                        playerSpawn = new GridIndex(x, z);
                        break;
                    case 'e':
                        enemies.Add(new EnemySpawn(new GridIndex(x, z), enemies.Count % 2));
                        break;
                }
            }
        }

        return new LevelMap(walls, playerSpawn, enemies);
    }

    public Vector3 CellToWorld(uint x, uint z, float y = 0f) =>
        new((x + 0.5f) * CellSize, y, (z + 0.5f) * CellSize);
}

internal readonly record struct EnemySpawn(GridIndex Cell, int SpriteIndex);
