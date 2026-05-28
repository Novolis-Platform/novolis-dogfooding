namespace MeshBench.Models;

internal sealed class MeshSceneDocument
{
    public List<MeshPartRecord> Parts { get; set; } = [];

    public OrbitCameraState Camera { get; set; } = new();
}

internal sealed class MeshPartRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Kind { get; set; } = "box";

    public float[] Center { get; set; } = [0f, 0.5f, 0f];

    public float[] HalfExtents { get; set; } = [0.5f, 0.5f, 0.5f];

    public float Radius { get; set; } = 0.5f;

    public float[] Color { get; set; } = [0.72f, 0.35f, 0.28f];
}

internal sealed class OrbitCameraState
{
    public float Yaw { get; set; } = 0.9f;

    public float Pitch { get; set; } = 0.35f;

    public float Distance { get; set; } = 4.5f;
}
