using Novolis.Math.Geometry;
using Novolis.Modeling.Import;
using Novolis.Modeling.Scene;

namespace CorellianFreighterBuilder;

/// <summary>
/// Bake Assimp imports (CGTrader FBX drop, etc.) into SceneLab .nov3djson.
/// </summary>
internal static class ExternalMeshImport
{
    public static EditableMesh Load(string path, float targetLengthMeters = 34.37f) =>
        AssimpMeshImporter.ImportEditable(path, new MeshImportOptions
        {
            TargetLengthMeters = targetLengthMeters,
            CenterAtOrigin = true,
            LongestAxisToPositiveZ = true,
            PreTransformVertices = true,
        });

    public static void WriteScene(EditableMesh mesh, string outPath, string displayName)
    {
        var doc = new SceneDocument
        {
            Name = displayName,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
        };
        var root = new GroupNode { Name = "Root" };
        var cam = new CameraNode
        {
            Name = "Camera",
            ParentId = root.Id,
            Transform = new SceneTransform { Position = [28f, 14f, 32f] },
            Target = [0, 0.2f, 2f],
            FovDeg = 36f,
        };
        var meshNode = new MeshNode
        {
            Name = displayName,
            ParentId = root.Id,
            Primitive = MeshPrimitiveKind.Box,
        };
        MeshEditBake.WriteBaked(meshNode, mesh);
        doc.Nodes.AddRange([
            root, cam, meshNode,
            new LightNode
            {
                Name = "Key",
                ParentId = root.Id,
                LightKind = LightKind.Spot,
                Intensity = 3.8f,
                Transform = new SceneTransform { Position = [22, 16, 18], RotationDeg = [40, -35, 0] },
            },
            new LightNode
            {
                Name = "Fill",
                ParentId = root.Id,
                LightKind = LightKind.Omni,
                Intensity = 2.0f,
                Color = [0.85f, 0.9f, 1f],
                Transform = new SceneTransform { Position = [0, 2f, -18f] },
            },
            new LightNode
            {
                Name = "Rim",
                ParentId = root.Id,
                LightKind = LightKind.Infinite,
                Intensity = 0.5f,
                Transform = new SceneTransform { RotationDeg = [-55, 30, 0] },
            },
        ]);
        doc.ActiveCameraId = null;
        doc.SelectionId = meshNode.Id;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
        SceneSerializer.Save(doc, outPath);
        Console.WriteLine($"IMPORT|{outPath}|verts={mesh.VertexCount}|tris={mesh.TriangleCount}");
    }
}
