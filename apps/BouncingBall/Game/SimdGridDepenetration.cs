using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace BouncingBall.Game;

/// <summary>Uniform-grid ball–ball separation with SIMD distance tests and cell-centric pair loops.</summary>
internal sealed class SimdGridDepenetration
{
    private const float MinDiameter = Ball.Radius * 2.001f;
    private const float MinDistSq = MinDiameter * MinDiameter;
    private const float BallBallRestitution = 0.88f;

    private readonly Dictionary<int, List<int>> _cells = new();
    private readonly Stack<List<int>> _cellPool = new();

    public readonly struct Result
    {
        public int PairChecks { get; init; }
        public int Contacts { get; init; }
    }

    public Result Resolve(
        BallSoA soa,
        float cellSize,
        bool applyImpulses,
        bool awakePairsOnly)
    {
        var pairChecks = 0;
        var contacts = 0;
        BuildGrid(soa, cellSize);

        foreach (var (key, indices) in _cells)
        {
            var cx = key / 4096;
            var cz = key % 4096;
            contacts += ResolveCellPairs(soa, indices, applyImpulses, awakePairsOnly, ref pairChecks);
            contacts += ResolveAcrossCells(soa, cx, cz, 1, 0, applyImpulses, awakePairsOnly, ref pairChecks);
            contacts += ResolveAcrossCells(soa, cx, cz, 0, 1, applyImpulses, awakePairsOnly, ref pairChecks);
            contacts += ResolveAcrossCells(soa, cx, cz, 1, 1, applyImpulses, awakePairsOnly, ref pairChecks);
            contacts += ResolveAcrossCells(soa, cx, cz, 1, -1, applyImpulses, awakePairsOnly, ref pairChecks);
        }

        return new Result { PairChecks = pairChecks, Contacts = contacts };
    }

    private int ResolveCellPairs(
        BallSoA soa,
        List<int> indices,
        bool applyImpulses,
        bool awakePairsOnly,
        ref int pairChecks)
    {
        var contacts = 0;
        var n = indices.Count;
        for (var a = 0; a < n; a++)
        {
            var i = indices[a];
            var j0 = a + 1;
            if (j0 >= n)
                continue;

            contacts += ResolveSimdBlock(soa, i, indices, j0, n, applyImpulses, awakePairsOnly, ref pairChecks);
        }

        return contacts;
    }

    private int ResolveAcrossCells(
        BallSoA soa,
        int cx,
        int cz,
        int dx,
        int dz,
        bool applyImpulses,
        bool awakePairsOnly,
        ref int pairChecks)
    {
        if (!_cells.TryGetValue(CellKey(cx + dx, cz + dz), out var other) || other.Count == 0)
            return 0;

        if (!_cells.TryGetValue(CellKey(cx, cz), out var self) || self.Count == 0)
            return 0;

        var contacts = 0;
        for (var a = 0; a < self.Count; a++)
        {
            var i = self[a];
            for (var b = 0; b < other.Count; b++)
            {
                var j = other[b];
                pairChecks++;
                if (TrySeparate(soa, i, j, applyImpulses, awakePairsOnly))
                    contacts++;
            }
        }

        return contacts;
    }

    private int ResolveSimdBlock(
        BallSoA soa,
        int i,
        List<int> indices,
        int jStart,
        int n,
        bool applyImpulses,
        bool awakePairsOnly,
        ref int pairChecks)
    {
        var contacts = 0;
        var px = soa.PosX[i];
        var py = soa.PosY[i];
        var pz = soa.PosZ[i];
        var minDistV = Vector128.Create(MinDistSq);
        var pxV = Vector128.Create(px);
        var pyV = Vector128.Create(py);
        var pzV = Vector128.Create(pz);

        var j = jStart;
        for (; j + 3 < n; j += 4)
        {
            pairChecks += 4;
            var j0 = indices[j];
            var j1 = indices[j + 1];
            var j2 = indices[j + 2];
            var j3 = indices[j + 3];

            var dx = Vector128.Create(soa.PosX[j0], soa.PosX[j1], soa.PosX[j2], soa.PosX[j3]) - pxV;
            var dy = Vector128.Create(soa.PosY[j0], soa.PosY[j1], soa.PosY[j2], soa.PosY[j3]) - pyV;
            var dz = Vector128.Create(soa.PosZ[j0], soa.PosZ[j1], soa.PosZ[j2], soa.PosZ[j3]) - pzV;
            var distSq = dx * dx + dy * dy + dz * dz;
            var hit = Vector128.LessThan(distSq, minDistV).AsInt32();

            if (hit.GetElement(0) != 0 && TrySeparate(soa, i, j0, applyImpulses, awakePairsOnly))
                contacts++;
            if (hit.GetElement(1) != 0 && TrySeparate(soa, i, j1, applyImpulses, awakePairsOnly))
                contacts++;
            if (hit.GetElement(2) != 0 && TrySeparate(soa, i, j2, applyImpulses, awakePairsOnly))
                contacts++;
            if (hit.GetElement(3) != 0 && TrySeparate(soa, i, j3, applyImpulses, awakePairsOnly))
                contacts++;
        }

        for (; j < n; j++)
        {
            pairChecks++;
            if (TrySeparate(soa, i, indices[j], applyImpulses, awakePairsOnly))
                contacts++;
        }

        return contacts;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TrySeparate(
        BallSoA soa,
        int i,
        int j,
        bool applyImpulses,
        bool awakePairsOnly)
    {
        if (awakePairsOnly && (soa.Sleeping[i] || soa.Sleeping[j]))
            return false;

        var dx = soa.PosX[j] - soa.PosX[i];
        var dy = soa.PosY[j] - soa.PosY[i];
        var dz = soa.PosZ[j] - soa.PosZ[i];
        var distSq = dx * dx + dy * dy + dz * dz;
        if (distSq >= MinDistSq)
            return false;

        float overlap;
        if (distSq < 1e-10f)
        {
            dx = 1f;
            dy = 0f;
            dz = 0f;
            overlap = MinDiameter;
        }
        else
        {
            var dist = MathF.Sqrt(distSq);
            var invDist = 1f / dist;
            overlap = MinDiameter - dist;
            dx *= invDist;
            dy *= invDist;
            dz *= invDist;
        }

        var push = overlap * 0.5f;
        soa.PosX[i] -= dx * push;
        soa.PosY[i] -= dy * push;
        soa.PosZ[i] -= dz * push;
        soa.PosX[j] += dx * push;
        soa.PosY[j] += dy * push;
        soa.PosZ[j] += dz * push;

        if (!applyImpulses || soa.Sleeping[i] || soa.Sleeping[j])
            return true;

        var rvx = soa.VelX[j] - soa.VelX[i];
        var rvy = soa.VelY[j] - soa.VelY[i];
        var rvz = soa.VelZ[j] - soa.VelZ[i];
        var vn = rvx * dx + rvy * dy + rvz * dz;
        if (vn >= 0f)
            return true;

        var impulse = -(1f + BallBallRestitution) * vn * 0.5f;
        soa.VelX[i] -= dx * impulse;
        soa.VelY[i] -= dy * impulse;
        soa.VelZ[i] -= dz * impulse;
        soa.VelX[j] += dx * impulse;
        soa.VelY[j] += dy * impulse;
        soa.VelZ[j] += dz * impulse;
        return true;
    }

    private void BuildGrid(BallSoA soa, float cellSize)
    {
        foreach (var list in _cells.Values)
        {
            list.Clear();
            _cellPool.Push(list);
        }

        _cells.Clear();

        var invCell = 1f / cellSize;
        for (var i = 0; i < soa.Count; i++)
        {
            var key = CellKey(
                (int)MathF.Floor(soa.PosX[i] * invCell),
                (int)MathF.Floor(soa.PosZ[i] * invCell));

            if (!_cells.TryGetValue(key, out var list))
            {
                list = _cellPool.Count > 0 ? _cellPool.Pop() : new List<int>(8);
                _cells[key] = list;
            }

            list.Add(i);
        }
    }

    private static int CellKey(int cellX, int cellZ) => cellX * 4096 + cellZ;
}
