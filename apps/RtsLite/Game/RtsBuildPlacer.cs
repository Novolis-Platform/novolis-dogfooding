using System.Drawing;
using System.Numerics;
using Novolis.Raylib.Game;

namespace RtsLite.Game;

internal sealed class RtsBuildPlacer
{
    public RtsBuildingType? ActiveType { get; private set; }

    public void Select(RtsBuildingType type) => ActiveType = type;

    public void Cancel() => ActiveType = null;

    public bool IsActive => ActiveType is not null;

    public void TryPlaceAtGround(RtsArena arena, Vector3 ground, UnitTeam team)
    {
        if (ActiveType is not { } type)
            return;

        arena.WorldToGrid(ground, out var gx, out var gz);
        if (arena.TryPlace(type, gx, gz, team))
            return;

        arena.WorldToGrid(ground - new Vector3(0.5f, 0f, 0.5f), out gx, out gz);
        arena.TryPlace(type, gx, gz, team);
    }

    public bool CanPlaceAt(RtsArena arena, Vector3 ground, UnitTeam team)
    {
        if (ActiveType is not { } type)
            return false;

        arena.WorldToGrid(ground, out var gx, out var gz);
        return arena.CanPlace(type, gx, gz, team);
    }

    public void DrawGhost(RayGameContext ctx, Novolis.Raylib.Rendering.Camera camera, RtsBuildingArt art, RtsArena arena, Vector3 ground, UnitTeam team)
    {
        if (ActiveType is not { } type)
            return;

        var ok = CanPlaceAt(arena, ground, team);
        var tint = ok
            ? Color.FromArgb(200, 120, 255, 140)
            : Color.FromArgb(200, 255, 90, 90);

        arena.WorldToGrid(ground, out var gx, out var gz);
        var center = arena.CellCenter(gx + type.FootprintW() / 2, gz + type.FootprintH() / 2);
        var scale = type switch
        {
            RtsBuildingType.ConstructionYard => 2.8f,
            RtsBuildingType.PowerPlant => 1.2f,
            RtsBuildingType.Barracks => 2.1f,
            RtsBuildingType.OreRefinery => 2.2f,
            RtsBuildingType.WarFactory => 2.4f,
            _ => 1.5f,
        };
        art.Draw(camera, ctx, type, center, scale, tint);

        var w = type.FootprintW() * RtsArena.CellSize;
        var h = type.FootprintH() * RtsArena.CellSize;
        var boxCenter = new Vector3(
            (gx + type.FootprintW() * 0.5f) * RtsArena.CellSize,
            0.05f,
            (gz + type.FootprintH() * 0.5f) * RtsArena.CellSize);
        ctx.DrawShipWires(boxCenter, new Vector3(w, 0.1f, h), tint);
    }
}
