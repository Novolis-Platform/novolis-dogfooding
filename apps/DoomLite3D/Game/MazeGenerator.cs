using Novolis.Math.Arrays;

namespace DoomLite3D.Game;

internal static class MazeGenerator
{
    private const int Size = 35;
    private const int LogicalSize = 11;
    private const int NodeSpacing = 3;
    private const int CalmRoomSize = 7;
    private const int CalmRoomOriginX = 1;
    private const int CalmRoomOriginZ = 1;
    private const int CalmExclusionRadius = 8;
    private const int MaxDiameter = 58;
    private const int MaxAttempts = 8;
    private const int MinEnemySpacing = 3;
    private const int MaxEnemies = 22;
    private const int CalmLogicalMax = 2;

    private static readonly (int Dx, int Dz)[] LogicalDirections = [(0, -1), (1, 0), (0, 1), (-1, 0)];

    public static (DenseGrid<byte> Walls, GridIndex PlayerSpawn, List<EnemySpawn> Enemies) Generate(int? seed = null)
    {
        var baseSeed = seed ?? Environment.TickCount;
        MazeAttempt? last = null;

        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var rng = new Random(HashSeed(baseSeed, attempt));
            var maze = TryGenerate(rng);
            if (maze is null)
                continue;

            if (maze.Value.Diameter <= MaxDiameter)
                return BuildLevel(maze.Value, rng);

            last = maze;
        }

        return BuildLevel(
            last ?? TryGenerate(new Random(baseSeed)) ?? throw new InvalidOperationException("Maze generation failed."),
            new Random(baseSeed));
    }

    private static MazeAttempt? TryGenerate(Random rng)
    {
        var walls = new byte[Size, Size];
        for (var z = 0; z < Size; z++)
        for (var x = 0; x < Size; x++)
            walls[x, z] = 1;

        var spawnX = CalmRoomOriginX + CalmRoomSize / 2;
        var spawnZ = CalmRoomOriginZ + CalmRoomSize / 2;

        CarveCalmRoom(walls);

        var visited = new bool[LogicalSize, LogicalSize];
        var openEast = new bool[LogicalSize, LogicalSize];
        var openSouth = new bool[LogicalSize, LogicalSize];

        for (var iz = 0; iz <= CalmLogicalMax; iz++)
        for (var ix = 0; ix <= CalmLogicalMax; ix++)
        {
            visited[ix, iz] = true;
            if (ix < CalmLogicalMax)
                openEast[ix, iz] = true;
            if (iz < CalmLogicalMax)
                openSouth[ix, iz] = true;
        }

        var stack = new Stack<(int X, int Z)>();
        stack.Push((CalmLogicalMax, CalmLogicalMax));

        while (stack.Count > 0)
        {
            var (cx, cz) = stack.Peek();
            var neighbors = new List<(int Nx, int Nz, int Dir)>();

            for (var dir = 0; dir < LogicalDirections.Length; dir++)
            {
                var (dx, dz) = LogicalDirections[dir];
                var nx = cx + dx;
                var nz = cz + dz;
                if (nx < 0 || nx >= LogicalSize || nz < 0 || nz >= LogicalSize)
                    continue;
                if (visited[nx, nz])
                    continue;
                neighbors.Add((nx, nz, dir));
            }

            if (neighbors.Count == 0)
            {
                stack.Pop();
                continue;
            }

            var pick = neighbors[rng.Next(neighbors.Count)];
            OpenPassage(openEast, openSouth, cx, cz, pick.Nx, pick.Nz, pick.Dir);
            visited[pick.Nx, pick.Nz] = true;
            stack.Push((pick.Nx, pick.Nz));
        }

        RasterizeLogicalMaze(walls, visited, openEast, openSouth);

        var diameter = ComputeMaxDistance(walls, spawnX, spawnZ);
        return new MazeAttempt(walls, spawnX, spawnZ, diameter);
    }

    private static void CarveCalmRoom(byte[,] walls)
    {
        for (var dz = 0; dz < CalmRoomSize; dz++)
        for (var dx = 0; dx < CalmRoomSize; dx++)
            walls[CalmRoomOriginX + dx, CalmRoomOriginZ + dz] = 0;

        var spawnZ = CalmRoomOriginZ + CalmRoomSize / 2;
        walls[CalmRoomOriginX + CalmRoomSize, spawnZ] = 0;
        walls[CalmRoomOriginX + CalmRoomSize, spawnZ + 1] = 0;
    }

    private static void OpenPassage(
        bool[,] openEast,
        bool[,] openSouth,
        int cx,
        int cz,
        int nx,
        int nz,
        int dir)
    {
        switch (dir)
        {
            case 1:
                openEast[cx, cz] = true;
                break;
            case 2:
                openSouth[cx, cz] = true;
                break;
            case 3:
                openEast[nx, cz] = true;
                break;
            default:
                openSouth[cx, nz] = true;
                break;
        }
    }

    private static void RasterizeLogicalMaze(
        byte[,] walls,
        bool[,] visited,
        bool[,] openEast,
        bool[,] openSouth)
    {
        for (var iz = 0; iz < LogicalSize; iz++)
        for (var ix = 0; ix < LogicalSize; ix++)
        {
            if (!visited[ix, iz])
                continue;

            var px = 1 + ix * NodeSpacing;
            var pz = 1 + iz * NodeSpacing;
            CarveNodeBlock(walls, px, pz);

            if (ix + 1 < LogicalSize && openEast[ix, iz])
                CarveLinkEast(walls, px, pz);

            if (iz + 1 < LogicalSize && openSouth[ix, iz])
                CarveLinkSouth(walls, px, pz);
        }
    }

    private static void CarveNodeBlock(byte[,] walls, int px, int pz)
    {
        for (var dz = 0; dz < 2; dz++)
        for (var dx = 0; dx < 2; dx++)
        {
            var x = px + dx;
            var z = pz + dz;
            if (x <= 0 || x >= Size - 1 || z <= 0 || z >= Size - 1)
                continue;
            walls[x, z] = 0;
        }
    }

    private static void CarveLinkEast(byte[,] walls, int px, int pz)
    {
        for (var dz = 0; dz < 2; dz++)
        for (var dx = 2; dx <= 3; dx++)
        {
            var x = px + dx;
            var z = pz + dz;
            if (x <= 0 || x >= Size - 1 || z <= 0 || z >= Size - 1)
                continue;
            walls[x, z] = 0;
        }
    }

    private static void CarveLinkSouth(byte[,] walls, int px, int pz)
    {
        for (var dz = 2; dz <= 3; dz++)
        for (var dx = 0; dx < 2; dx++)
        {
            var x = px + dx;
            var z = pz + dz;
            if (x <= 0 || x >= Size - 1 || z <= 0 || z >= Size - 1)
                continue;
            walls[x, z] = 0;
        }
    }

    private static int ComputeMaxDistance(byte[,] walls, int spawnX, int spawnZ)
    {
        var dist = new int[Size, Size];
        for (var z = 0; z < Size; z++)
        for (var x = 0; x < Size; x++)
            dist[x, z] = -1;

        var queue = new Queue<(int X, int Z)>();
        queue.Enqueue((spawnX, spawnZ));
        dist[spawnX, spawnZ] = 0;
        var max = 0;

        while (queue.Count > 0)
        {
            var (x, z) = queue.Dequeue();
            var d = dist[x, z];
            max = System.Math.Max(max, d);

            foreach (var (nx, nz) in new[] { (x - 1, z), (x + 1, z), (x, z - 1), (x, z + 1) })
            {
                if (nx < 0 || nx >= Size || nz < 0 || nz >= Size)
                    continue;
                if (walls[nx, nz] != 0 || dist[nx, nz] >= 0)
                    continue;
                dist[nx, nz] = d + 1;
                queue.Enqueue((nx, nz));
            }
        }

        return max;
    }

    private static (DenseGrid<byte> Walls, GridIndex PlayerSpawn, List<EnemySpawn> Enemies) BuildLevel(
        MazeAttempt maze,
        Random rng)
    {
        var grid = new DenseGrid<byte>((uint)Size, (uint)Size);
        for (var z = 0; z < Size; z++)
        for (var x = 0; x < Size; x++)
            grid.Set((uint)x, (uint)z, maze.Walls[x, z]);

        var spawn = new GridIndex((uint)maze.SpawnX, (uint)maze.SpawnZ);
        var enemies = PlaceEnemies(maze.Walls, spawn, rng);
        return (grid, spawn, enemies);
    }

    private static List<EnemySpawn> PlaceEnemies(byte[,] walls, GridIndex spawn, Random rng)
    {
        var eligible = new List<GridIndex>();
        for (var z = 0; z < Size; z++)
        for (var x = 0; x < Size; x++)
        {
            if (walls[x, z] != 0)
                continue;
            var dist = System.Math.Abs(x - (int)spawn.X) + System.Math.Abs(z - (int)spawn.Y);
            if (dist < CalmExclusionRadius)
                continue;
            eligible.Add(new GridIndex((uint)x, (uint)z));
        }

        Shuffle(eligible, rng);
        var target = System.Math.Min(MaxEnemies, System.Math.Max(4, eligible.Count * 25 / 100));
        var placed = new List<EnemySpawn>();
        var positions = new List<(int X, int Z)>();

        foreach (var cell in eligible)
        {
            if (placed.Count >= target)
                break;

            var cx = (int)cell.X;
            var cz = (int)cell.Y;
            if (positions.Any(p => System.Math.Abs(p.X - cx) + System.Math.Abs(p.Z - cz) < MinEnemySpacing))
                continue;

            positions.Add((cx, cz));
            placed.Add(new EnemySpawn(cell, placed.Count % 2));
        }

        return placed;
    }

    private static void Shuffle<T>(List<T> list, Random rng)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static int HashSeed(int baseSeed, int attempt) =>
        unchecked(baseSeed * 31 + attempt);

    private readonly record struct MazeAttempt(byte[,] Walls, int SpawnX, int SpawnZ, int Diameter);
}
