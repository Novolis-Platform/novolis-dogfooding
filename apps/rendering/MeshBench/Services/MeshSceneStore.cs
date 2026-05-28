using System.IO.Abstractions;
using System.Numerics;
using System.Text.Json;
using MeshBench.Models;
using Novolis.Rendering.Compile;
using Novolis.Rendering.Runtime;
using Novolis.Rendering.Scene;
using Novolis.Workspaces;
using Novolis.Workspaces.FileSystem;

namespace MeshBench.Services;

internal sealed class MeshSceneStore
{
    private readonly IFileSystem _fileSystem;
    private readonly JsonSerializerOptions _json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };

    public MeshSceneStore(IFileSystem fileSystem) => _fileSystem = fileSystem;

    public string ScenePath(IProject project) =>
        _fileSystem.Path.Combine(project.Root.FullName, WorkspaceLayout.DocumentsFolder, "scene.json");

    public MeshSceneDocument Load(IProject project)
    {
        var path = ScenePath(project);
        if (!_fileSystem.File.Exists(path))
            return CreateDefault();

        var json = _fileSystem.File.ReadAllText(path);
        return JsonSerializer.Deserialize<MeshSceneDocument>(json, _json) ?? CreateDefault();
    }

    public void Save(IProject project, MeshSceneDocument document)
    {
        var path = ScenePath(project);
        _fileSystem.Directory.CreateDirectory(_fileSystem.Path.GetDirectoryName(path)!);
        _fileSystem.File.WriteAllText(path, JsonSerializer.Serialize(document, _json));
    }

    public CompiledScene Compile(MeshSceneDocument document)
    {
        var builder = new SceneBuilder();
        MeshPrimitives.AddGroundAndLight(builder);
        foreach (var part in document.Parts)
        {
            var center = ToVector3(part.Center);
            var color = ToVector3(part.Color);
            if (part.Kind.Equals("sphere", StringComparison.OrdinalIgnoreCase))
                MeshPrimitives.AddSphere(builder, center, part.Radius, color);
            else
                MeshPrimitives.AddBox(builder, center, ToVector3(part.HalfExtents), color);
        }

        return SceneCompiler.Compile(builder.Build());
    }

    private static MeshSceneDocument CreateDefault()
    {
        var doc = new MeshSceneDocument();
        doc.Parts.Add(new MeshPartRecord
        {
            Kind = "box",
            Center = [0f, 0.5f, 0f],
            HalfExtents = [0.5f, 0.5f, 0.5f],
            Color = [0.72f, 0.35f, 0.28f],
        });
        doc.Parts.Add(new MeshPartRecord
        {
            Kind = "sphere",
            Center = [1.4f, 0.55f, 0.2f],
            Radius = 0.45f,
            Color = [0.28f, 0.55f, 0.82f],
        });
        return doc;
    }

    private static Vector3 ToVector3(float[] values) =>
        values.Length >= 3 ? new Vector3(values[0], values[1], values[2]) : Vector3.Zero;
}
