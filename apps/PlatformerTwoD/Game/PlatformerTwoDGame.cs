using System.Numerics;
using Novolis.Math.Geometry;
using Novolis.Rendering.Backends.TwoD.Silk;
using Novolis.Rendering.TwoD;
using PlatformerHop.Game;
using Silk.NET.Input;

namespace PlatformerTwoD.Game;

internal sealed class PlatformerTwoDGame
{
    private readonly PlanarHopPlayer _player = new();
    private readonly SideLevel _level = SideLevel.CreateDemo();
    private TwoDStaticPolygon? _playerMarker;
    private float _cameraX;

    public void Initialize(SilkTwoDGameContext ctx)
    {
        var scene = ctx.Scene;
        scene.Camera.ClearColor = new Rgba32(30, 36, 52);
        scene.Camera.WorldUnitsPerPixel = 1f / 32f;
        BuildPlatforms(scene);
        _player.Reset(_level);
        _cameraX = _player.Position.X;
    }

    public void Update(SilkTwoDGameContext ctx)
    {
        var scene = ctx.Scene;
        var move = 0f;
        if (ctx.IsKeyDown(Key.A))
        {
            move -= 1f;
        }

        if (ctx.IsKeyDown(Key.D))
        {
            move += 1f;
        }

        var jump = ctx.IsKeyPressed(Key.Space) || ctx.IsKeyPressed(Key.W);
        if (ctx.IsKeyPressed(Key.R))
        {
            _player.Reset(_level);
        }

        _player.Update(_level, move, jump, ctx.DeltaSeconds);

        var targetX = _player.Position.X;
        var t = 1f - MathF.Exp(-8f * ctx.DeltaSeconds);
        _cameraX = float.Lerp(_cameraX, targetX, t);
        scene.Camera.Position = Vector3PlanarExtensions.Xz(_cameraX, _player.Position.Z + 1.5f);

        scene.Update(ctx.DeltaSeconds);
        scene.Hud.Elements.Clear();
        scene.Hud.AddText("A/D move  |  Space/W jump  |  R reset", 12, 12, 2f, new Rgba32(210, 220, 235));
        scene.Hud.AddText(
            $"pos {_player.Position.X:F1},{_player.Position.Z:F1}  vz {_player.VelocityZ:F2}",
            12,
            36,
            2f,
            new Rgba32(180, 200, 220));

        if (_playerMarker is not null)
        {
            scene.StaticPolygons.Remove(_playerMarker);
        }

        _playerMarker = CreatePlayerMarker(_player.Position, SideLevel.PlayerRadius);
        scene.StaticPolygons.Add(_playerMarker);
    }

    private void BuildPlatforms(TwoDScene scene)
    {
        var tiles = _level.Tiles;
        var cell = SideLevel.CellSize;
        var fill = new Rgba32(90, 110, 150);

        for (var z = 0u; z < tiles.Height; z++)
        for (var x = 0u; x < tiles.Width; x++)
        {
            if (tiles[x, z, 0] == 0)
            {
                continue;
            }

            var minX = x * cell;
            var minZ = z * cell;
            scene.AddPlatform(minX, minZ, minX + cell, minZ + cell, fill);
        }
    }

    private static TwoDStaticPolygon CreatePlayerMarker(Vector3 pos, float radius)
    {
        return new TwoDStaticPolygon(
            TwoDScenePrimitives.Rectangle(
                pos.X - radius,
                pos.Z - radius,
                pos.X + radius,
                pos.Z + radius),
            new Rgba32(255, 180, 90))
        {
            DrawFilled = true,
            DrawOutline = true,
            SortKey = 1000,
        };
    }
}
