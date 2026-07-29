using System.Text.Json;
using System.Text.Json.Serialization;

namespace CalypsoCad.Models;

internal static class CadJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };
}

internal sealed class CadDocument
{
    public string Format { get; set; } = "novolis.cad";
    public int SchemaVersion { get; set; } = 1;
    public string Name { get; set; } = "Untitled";
    public CadGenerator Generator { get; set; } = new();
    public string? CreatedAt { get; set; }
    public string? ModifiedAt { get; set; }
    public float UnitScaleMeters { get; set; } = 1f;
    public string LinearUnit { get; set; } = "meter";
    public string AngleUnit { get; set; } = "radian";
    public CadCoordinateSystem CoordinateSystem { get; set; } = new();
    public string? LayersDocument { get; set; }
    public string? ShapesDocument { get; set; }
    public List<CadLayer> Layers { get; set; } = [];
    public List<CadLinetype> Linetypes { get; set; } = [new() { Name = "Continuous" }];
    public List<CadShape> Shapes { get; set; } = [];
    public List<CadEntity> Entities { get; set; } = [];
    public CadCamera Camera { get; set; } = new();
    public Dictionary<string, JsonElement>? Properties { get; set; }
}

internal sealed class CadGenerator
{
    public string Name { get; set; } = "CalypsoCad";
    public string Version { get; set; } = "2026.1.0";
}

internal sealed class CadCoordinateSystem
{
    public string Handedness { get; set; } = "right";
    public string UpAxis { get; set; } = "y";
    public string ForwardAxis { get; set; } = "z";
}

internal sealed class CadLayer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "0";
    public Guid? CatalogId { get; set; }
    public bool Visible { get; set; } = true;
    public bool Locked { get; set; }
    public float[]? Color { get; set; }
}

internal sealed class CadLinetype
{
    public string Name { get; set; } = "Continuous";
    public float[]? Pattern { get; set; }
}

internal sealed class CadStyle
{
    public string Linetype { get; set; } = "Continuous";
    public float LineWeightMm { get; set; }
    public float[]? Color { get; set; }
    public int? ColorIndex { get; set; }
}

internal sealed class CadCamera
{
    public float Yaw { get; set; } = 0.9f;
    public float Pitch { get; set; } = 0.45f;
    public float Distance { get; set; } = 80f;
    public float[] Target { get; set; } = [0f, 4f, 0f];
}

internal sealed class CadWallSide
{
    public Guid? ShapeId { get; set; }
}

internal sealed class CadWallSides
{
    public CadWallSide? A { get; set; }
    public CadWallSide? B { get; set; }
}

internal sealed class CadHook
{
    public Guid Id { get; set; }
    public string Tag { get; set; } = "";
    public float[]? Position { get; set; }
    public float[]? Normal { get; set; }
    public Dictionary<string, JsonElement>? Properties { get; set; }
}

internal sealed class CadSpaceFlags
{
    public bool Enclosed { get; set; }
    public bool Hollow { get; set; }
}

internal sealed class CadTransform
{
    public float[] Center { get; set; } = [0f, 0f, 0f];
    public float? RotationY { get; set; }
    public float[]? RotationQuat { get; set; }
    public float[]? Scale { get; set; }
}

internal sealed class CadOpeningSwing
{
    public float StartAngle { get; set; }
    public float EndAngle { get; set; }
    public float[] Direction { get; set; } = [0f, 0f, 1f];
}

internal sealed class CadOpening
{
    public string? OpeningType { get; set; }
    public int Deck { get; set; }
    public float Height { get; set; }
    public List<float[]>? Footprint { get; set; }
    public Guid? HostWallId { get; set; }
    public List<string>? ConnectsSides { get; set; }
    public CadOpeningSwing? Swing { get; set; }
}

internal sealed class CadBoolean
{
    public string? Operation { get; set; }
    public Guid? LeftId { get; set; }
    public Guid? RightId { get; set; }
    public string? Mode { get; set; }
    public float? TouchEpsilonMeters { get; set; }
}

internal sealed class CadWeld
{
    public List<Guid>? MemberIds { get; set; }
    public float TouchEpsilonMeters { get; set; }
}

internal sealed class CadEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Name { get; set; }
    public Guid? LayerId { get; set; }
    public Guid? ParentId { get; set; }
    public string Kind { get; set; } = "line";
    public List<CadHook>? Hooks { get; set; }
    public Guid? ShapeId { get; set; }
    public CadStyle? Style { get; set; }
    public string? Material { get; set; }
    public float[]? Color { get; set; }
    public float[]? A { get; set; }
    public float[]? B { get; set; }
    public List<float[]>? Points { get; set; }
    public float Thickness { get; set; }
    public float Height { get; set; }
    public int Deck { get; set; }
    public CadWallSides? Sides { get; set; }
    public Guid? FloorShapeId { get; set; }
    public Guid? CeilingShapeId { get; set; }

    // opening kind payload
    public string? OpeningType { get; set; }
    public List<float[]>? Footprint { get; set; }
    public Guid? HostWallId { get; set; }
    public List<string>? ConnectsSides { get; set; }
    public CadOpeningSwing? Swing { get; set; }

    // boolean kind payload
    public string? Operation { get; set; }
    public Guid? LeftId { get; set; }
    public Guid? RightId { get; set; }
    public string? Mode { get; set; }
    public float? TouchEpsilonMeters { get; set; }

    // weld kind payload
    public List<Guid>? MemberIds { get; set; }

    // instance kind payload
    public Guid? PrototypeId { get; set; }
    public CadTransform? Transform { get; set; }

    // arrayInstance kind payload
    public CadTransform? BaseTransform { get; set; }
    public int[]? Counts { get; set; }
    public float[]? Spacing { get; set; }

    // space.flags payload
    public CadSpaceFlags? Flags { get; set; }

    public Dictionary<string, JsonElement>? Properties { get; set; }
}

internal sealed class CadLayersDocument
{
    public string Format { get; set; } = "novolis.cad.layers";
    public int SchemaVersion { get; set; } = 1;
    public string Name { get; set; } = "layers";
    public string Standard { get; set; } = "custom";
    public string? StandardVersion { get; set; }
    public CadGenerator Generator { get; set; } = new();
    public string? CreatedAt { get; set; }
    public string? ModifiedAt { get; set; }
    public List<CadCatalogLayer> Layers { get; set; } = [];
}

internal sealed class CadCatalogLayer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string? Discipline { get; set; }
    public string? Major { get; set; }
    public List<string>? Minor { get; set; }
    public string? Description { get; set; }
    public float[]? DefaultColor { get; set; }
    public float DefaultLineWeightMm { get; set; }
    public string DefaultLinetype { get; set; } = "Continuous";
    public bool Plot { get; set; } = true;
}

internal sealed class CadShapesDocument
{
    public string Format { get; set; } = "novolis.cad.shape";
    public int SchemaVersion { get; set; } = 1;
    public string Name { get; set; } = "shapes";
    public CadGenerator Generator { get; set; } = new();
    public string? CreatedAt { get; set; }
    public string? ModifiedAt { get; set; }
    public string? BaseDocument { get; set; }
    public List<CadShape> Shapes { get; set; } = [];
}

internal sealed class CadShape
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Name { get; set; }
    public CadShapeExtensions? Extensions { get; set; }
}

internal sealed class CadShapeExtensions
{
    public CadAppearanceExtension? Appearance { get; set; }
    public CadMaterialExtension? Material { get; set; }
}

internal sealed class CadAppearanceExtension
{
    public CadFill? Fill { get; set; }
    public CadStroke? Stroke { get; set; }
    public int? ColorIndex { get; set; }
}

internal sealed class CadFill
{
    public bool Enabled { get; set; } = true;
    public float[]? Color { get; set; }
}

internal sealed class CadStroke
{
    public float[]? Color { get; set; }
    public float LineWeightMm { get; set; }
    public string Linetype { get; set; } = "Continuous";
}

internal sealed class CadMaterialExtension
{
    public string? Preset { get; set; }
    public float[]? Albedo { get; set; }
    public float Roughness { get; set; } = 0.5f;
    public float Metalness { get; set; }
}
