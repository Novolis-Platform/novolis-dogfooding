using System.Drawing;
using System.Numerics;
using Novolis.Physics.Collision.Simple;
using Novolis.Raylib.Game;

namespace ArtillerySimulator.Game;

/// <summary>Procedural heightfield with logical range box and ground-projected edge impacts.</summary>
internal sealed class TerrainWorld
{
    public float ExtentMeters => SimulationUnits.ExtentMeters;
    private const int CollisionCells = 256;
    private const int DrawCells = 128;

    private static readonly Color BoundaryColor = Color.FromArgb(255, 200, 180, 90);
    private static readonly Color GroundFill = Color.FromArgb(28, 22, 38, 30);

    private readonly Color _wireLow = Color.FromArgb(255, 58, 88, 52);
    private readonly Color _wireHigh = Color.FromArgb(255, 88, 132, 68);

    private BvhStaticWorld _collision = null!;
    private Vector3[] _drawVertices = [];
    private int _seed;
    private bool _flat;
    private TerrainStyle _style = TerrainStyle.Rugged;

    public BvhStaticWorld Collision => _collision;
    public Vector3 GunBaseline { get; private set; }
    public bool IsFlat => _flat;
    public TerrainStyle Style => _style;

    public void Rebuild(int seed, bool flat, TerrainStyle style = TerrainStyle.Rugged)
    {
        _seed = seed;
        _flat = flat;
        _style = style;
        BuildMeshes();
        GunBaseline = new Vector3(
            SimulationUnits.GunPivotX,
            SampleHeight(SimulationUnits.GunPivotX, ExtentMeters * 0.5f) + SimulationUnits.GunHeightOffset,
            ExtentMeters * 0.5f);
    }

    public bool IsInside(float x, float z) =>
        x >= 0f && x <= ExtentMeters && z >= 0f && z <= ExtentMeters;

    public bool TryHeightfieldContact(Vector3 position, float radius)
    {
        if (!IsInside(position.X, position.Z))
            return false;

        return position.Y <= SampleHeight(position.X, position.Z) + radius;
    }

    /// <summary>Clamp XZ to the range box and place Y on the terrain surface.</summary>
    public Vector3 ProjectOntoTerrainSurface(Vector3 p, float surfaceEpsilon = 0.05f)
    {
        var x = Math.Clamp(p.X, 0f, ExtentMeters);
        var z = Math.Clamp(p.Z, 0f, ExtentMeters);
        var y = SampleHeight(x, z) + surfaceEpsilon;
        return new Vector3(x, y, z);
    }

    /// <summary>First exit of segment from the XZ range box (0…extent). Returns false if both endpoints inside.</summary>
    public bool TrySegmentLeavesRange(
        Vector3 from,
        Vector3 to,
        out Vector3 hitPoint,
        out float fractionAlongSegment)
    {
        hitPoint = default;
        fractionAlongSegment = 1f;

        if (IsInside(from.X, from.Z) && IsInside(to.X, to.Z))
            return false;

        var delta = to - from;
        var tBest = 1f;
        var found = false;

        if (MathF.Abs(delta.X) > 1e-8f)
        {
            if (delta.X > 0f && from.X <= ExtentMeters)
                TryPlane(from.X, ExtentMeters, delta.X, ref tBest, ref found);
            if (delta.X < 0f && from.X >= 0f)
                TryPlane(from.X, 0f, delta.X, ref tBest, ref found);
        }

        if (MathF.Abs(delta.Z) > 1e-8f)
        {
            if (delta.Z > 0f && from.Z <= ExtentMeters)
                TryPlane(from.Z, ExtentMeters, delta.Z, ref tBest, ref found);
            if (delta.Z < 0f && from.Z >= 0f)
                TryPlane(from.Z, 0f, delta.Z, ref tBest, ref found);
        }

        if (!found || tBest < 0f || tBest > 1f)
            return false;

        fractionAlongSegment = tBest;
        hitPoint = from + delta * tBest;
        return true;
    }

    private static void TryPlane(float start, float plane, float delta, ref float tBest, ref bool found)
    {
        var t = (plane - start) / delta;
        if (t < 0f || t > 1f || t >= tBest)
            return;

        tBest = t;
        found = true;
    }

    public float SampleHeight(float x, float z)
    {
        if (_flat)
            return 0f;

        var extent = ExtentMeters;
        return TerrainHeightSampler.Sample(x, z, _seed, _style);
    }

    public void Draw(RayGameContext ctx)
    {
        var e = ExtentMeters;
        ctx.DrawPlane(new Vector3(e * 0.5f, 0f, e * 0.5f), new Vector2(e, e), GroundFill);

        var stride = DrawCells + 1;
        for (var z = 0; z < DrawCells; z++)
        {
            for (var x = 0; x < DrawCells; x++)
            {
                var i00 = z * stride + x;
                var i10 = i00 + 1;
                var i01 = i00 + stride;
                var i11 = i01 + 1;
                var a = _drawVertices[i00];
                var b = _drawVertices[i10];
                var c = _drawVertices[i01];
                var d = _drawVertices[i11];
                var midY = (a.Y + b.Y + c.Y + d.Y) * 0.25f;
                var color = midY < 180f ? _wireLow : midY < 420f ? _wireHigh : Color.FromArgb(255, 118, 168, 92);
                ctx.DrawBolt(a, b, color);
                ctx.DrawBolt(a, c, color);
            }
        }

        for (var x = 0; x <= DrawCells; x++)
        {
            for (var z = 0; z < DrawCells; z++)
            {
                var a = _drawVertices[z * stride + x];
                var b = _drawVertices[(z + 1) * stride + x];
                var midY = (a.Y + b.Y) * 0.5f;
                var color = midY < 180f ? _wireLow : midY < 420f ? _wireHigh : Color.FromArgb(255, 118, 168, 92);
                ctx.DrawBolt(a, b, color);
            }
        }

        DrawRangeBoundary(ctx);
    }

    private void DrawRangeBoundary(RayGameContext ctx)
    {
        var e = ExtentMeters;
        var y = MathF.Max(520f, ExtentMeters * 0.03f);
        var a = new Vector3(0f, y, 0f);
        var b = new Vector3(e, y, 0f);
        var c = new Vector3(e, y, e);
        var d = new Vector3(0f, y, e);
        ctx.DrawBolt(a, b, BoundaryColor);
        ctx.DrawBolt(b, c, BoundaryColor);
        ctx.DrawBolt(c, d, BoundaryColor);
        ctx.DrawBolt(d, a, BoundaryColor);
    }

    private void BuildMeshes()
    {
        var extent = ExtentMeters;
        var verts = new Vector3[(CollisionCells + 1) * (CollisionCells + 1)];
        var tris = new List<int>(CollisionCells * CollisionCells * 6);

        for (var z = 0; z <= CollisionCells; z++)
        for (var x = 0; x <= CollisionCells; x++)
        {
            var fx = x / (float)CollisionCells * extent;
            var fz = z / (float)CollisionCells * extent;
            verts[z * (CollisionCells + 1) + x] = new Vector3(fx, SampleHeight(fx, fz), fz);
        }

        for (var z = 0; z < CollisionCells; z++)
        for (var x = 0; x < CollisionCells; x++)
        {
            var i00 = z * (CollisionCells + 1) + x;
            var i10 = i00 + 1;
            var i01 = i00 + (CollisionCells + 1);
            var i11 = i01 + 1;
            tris.Add(i00);
            tris.Add(i10);
            tris.Add(i01);
            tris.Add(i10);
            tris.Add(i11);
            tris.Add(i01);
        }

        _collision = new BvhStaticWorld(new StaticTriangleMesh(verts, tris.ToArray()));

        _drawVertices = new Vector3[(DrawCells + 1) * (DrawCells + 1)];
        for (var z = 0; z <= DrawCells; z++)
        for (var x = 0; x <= DrawCells; x++)
        {
            var fx = x / (float)DrawCells * extent;
            var fz = z / (float)DrawCells * extent;
            _drawVertices[z * (DrawCells + 1) + x] = new Vector3(fx, SampleHeight(fx, fz), fz);
        }
    }
}
