using System.Drawing;
using System.Numerics;
using Novolis.Physics.Collision.Simple;
using Novolis.Raylib.Game;

namespace ArtillerySimulator.Game;

/// <summary>Procedural heightfield meshed for BVH collision and wireframe draw.</summary>
internal sealed class TerrainWorld
{
    public const float ExtentMeters = 500f;
    private const int GridCells = 64;

    private readonly Color _wireLow = Color.FromArgb(255, 48, 72, 44);
    private readonly Color _wireHigh = Color.FromArgb(255, 72, 110, 58);

    private BvhStaticWorld _collision = null!;
    private Vector3[] _vertices = [];
    private int[] _triangleIndices = [];
    private int _seed;
    private bool _flat;

    public BvhStaticWorld Collision => _collision;
    public Vector3 GunBaseline { get; private set; }
    public bool IsFlat => _flat;

    public void Rebuild(int seed, bool flat)
    {
        _seed = seed;
        _flat = flat;
        BuildMesh();
        GunBaseline = new Vector3(40f, SampleHeight(40f, ExtentMeters * 0.5f) + 1.2f, ExtentMeters * 0.5f);
    }

    public float SampleHeight(float x, float z)
    {
        if (_flat)
            return 0f;

        var nx = x / ExtentMeters * MathF.Tau * 1.4f + _seed * 0.017f;
        var nz = z / ExtentMeters * MathF.Tau * 1.1f + _seed * 0.023f;
        return 18f
               + 28f * MathF.Sin(nx)
               + 22f * MathF.Cos(nz * 0.85f)
               + 14f * MathF.Sin((nx + nz) * 0.55f);
    }

    public void Draw(RayGameContext ctx)
    {
        var triCount = _triangleIndices.Length / 3;
        for (var t = 0; t < triCount; t++)
        {
            var i = t * 3;
            var a = _vertices[_triangleIndices[i]];
            var b = _vertices[_triangleIndices[i + 1]];
            var c = _vertices[_triangleIndices[i + 2]];
            var midY = (a.Y + b.Y + c.Y) / 3f;
            var color = midY < 25f ? _wireLow : _wireHigh;
            ctx.DrawBolt(a, b, color);
            ctx.DrawBolt(b, c, color);
            ctx.DrawBolt(c, a, color);
        }
    }

    private void BuildMesh()
    {
        var verts = new Vector3[(GridCells + 1) * (GridCells + 1)];
        var tris = new List<int>(GridCells * GridCells * 6);

        for (var z = 0; z <= GridCells; z++)
        for (var x = 0; x <= GridCells; x++)
        {
            var fx = x / (float)GridCells * ExtentMeters;
            var fz = z / (float)GridCells * ExtentMeters;
            verts[z * (GridCells + 1) + x] = new Vector3(fx, SampleHeight(fx, fz), fz);
        }

        for (var z = 0; z < GridCells; z++)
        for (var x = 0; x < GridCells; x++)
        {
            var i00 = z * (GridCells + 1) + x;
            var i10 = i00 + 1;
            var i01 = i00 + (GridCells + 1);
            var i11 = i01 + 1;
            tris.Add(i00);
            tris.Add(i10);
            tris.Add(i01);
            tris.Add(i10);
            tris.Add(i11);
            tris.Add(i01);
        }

        _vertices = verts;
        _triangleIndices = tris.ToArray();
        _collision = new BvhStaticWorld(new StaticTriangleMesh(_vertices, _triangleIndices));
    }
}
