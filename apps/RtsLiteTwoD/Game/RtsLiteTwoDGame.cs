using System.Numerics;
using Novolis.Dogfooding.TwoD;
using Novolis.Math.Geometry;
using Novolis.Rendering.Backends.TwoD.Silk;
using Novolis.Rendering.TwoD;
using RtsLite.Game;
using Silk.NET.Input;

namespace RtsLiteTwoD.Game;

internal sealed class RtsLiteTwoDGame
{
    private static readonly Rgba32 Sky = new(30, 36, 52);
    private static readonly Rgba32 Sand = new(168, 142, 88);
    private static readonly Rgba32 SandDark = new(138, 112, 68);
    private static readonly Rgba32 Water = new(48, 110, 118);
    private static readonly Rgba32 Tiberium = new(58, 168, 62);
    private static readonly Rgba32 WallRock = new(90, 78, 62);
    private static readonly Rgba32 AlliedBuilding = new(72, 118, 188);
    private static readonly Rgba32 SovietBuilding = new(188, 62, 52);
    private static readonly Rgba32 AlliedUnit = new(100, 160, 255);
    private static readonly Rgba32 SovietUnit = new(255, 100, 80);
    private static readonly Rgba32 OrderLine = new(255, 240, 120, 180);
    private static readonly Rgba32 HudText = new(235, 228, 200);

    private readonly OrthoPanCamera _camera = new();
    private readonly RtsSelection _selection = new();
    private readonly RtsBuildPlacer _build = new();
    private readonly List<RtsUnit> _units = [];
    private readonly List<TwoDStaticPolygon> _dynamicMarkers = [];

    private RtsArena _arena = null!;
    private float _enemyPulse;
    private float _spawnPulse;
    private bool _terrainBuilt;

    public void Initialize(SilkTwoDGameContext ctx)
    {
        _arena = RtsArena.Create();
        SpawnForces();
        _camera.Center = _arena.CellCenter(RtsArena.GridSize / 2, RtsArena.GridSize / 2);
        _camera.ApplyTo(ctx.Scene.Camera, ctx.Width, ctx.Height);
        ctx.Scene.Camera.ClearColor = Sky;
    }

    public void Update(SilkTwoDGameContext ctx)
    {
        var scene = ctx.Scene;
        UpdateCamera(ctx);
        HandleBuildKeys(ctx);

        var ground = ScreenToGround(scene.Camera, ctx, PivotScreen(ctx));
        HandleSelection(ctx, ground);
        TickUnits(ctx.DeltaSeconds);
        TickProduction(ctx.DeltaSeconds);

        if (!_terrainBuilt)
        {
            BuildTerrain(scene);
            DrawBuildings(scene);
            _terrainBuilt = true;
        }

        DrawBuildGhost(scene, ground);
        DrawUnits(scene);
        DrawHud(ctx);
    }

    private void DrawBuildGhost(TwoDScene scene, Vector3 ground)
    {
        if (!_build.TryGetGhostFootprint(_arena, ground, UnitTeam.Player, out var center, out var w, out var d, out var ok))
        {
            return;
        }

        var color = ok ? new Rgba32(120, 255, 140, 200) : new Rgba32(255, 90, 90, 200);
        scene.StaticPolygons.Add(new TwoDStaticPolygon(
            TwoDScenePrimitives.Rectangle(center.X - w * 0.5f, center.Z - d * 0.5f, center.X + w * 0.5f, center.Z + d * 0.5f),
            color)
        {
            DrawFilled = true,
            DrawOutline = true,
            SortKey = 1100,
        });
    }

    private void UpdateCamera(SilkTwoDGameContext ctx)
    {
        var dt = ctx.DeltaSeconds;
        var pan = 12f * dt / _camera.WorldUnitsPerPixel;
        if (ctx.IsKeyDown(Key.W))
        {
            _camera.Pan(new Vector3(0f, 0f, -pan));
        }

        if (ctx.IsKeyDown(Key.S))
        {
            _camera.Pan(new Vector3(0f, 0f, pan));
        }

        if (ctx.IsKeyDown(Key.A))
        {
            _camera.Pan(new Vector3(-pan, 0f, 0f));
        }

        if (ctx.IsKeyDown(Key.D))
        {
            _camera.Pan(new Vector3(pan, 0f, 0f));
        }

        if (ctx.IsKeyPressed(Key.Equal) || ctx.IsKeyPressed(Key.KeypadAdd))
        {
            _camera.Zoom(-1f);
        }

        if (ctx.IsKeyPressed(Key.Minus) || ctx.IsKeyPressed(Key.KeypadSubtract))
        {
            _camera.Zoom(1f);
        }

        _camera.ApplyTo(ctx.Scene.Camera, ctx.Width, ctx.Height);
    }

    private void HandleBuildKeys(SilkTwoDGameContext ctx)
    {
        if (ctx.IsKeyPressed(Key.Number1))
        {
            _build.Select(RtsBuildingType.ConstructionYard);
        }

        if (ctx.IsKeyPressed(Key.Number2))
        {
            _build.Select(RtsBuildingType.PowerPlant);
        }

        if (ctx.IsKeyPressed(Key.Number3))
        {
            _build.Select(RtsBuildingType.Barracks);
        }

        if (ctx.IsKeyPressed(Key.Number4))
        {
            _build.Select(RtsBuildingType.OreRefinery);
        }

        if (ctx.IsKeyPressed(Key.Number5))
        {
            _build.Select(RtsBuildingType.WarFactory);
        }

        if (ctx.IsKeyPressed(Key.B))
        {
            _build.Cancel();
        }
    }

    private void HandleSelection(SilkTwoDGameContext ctx, Vector3 ground)
    {
        var mouse = PivotScreen(ctx);
        var leftPressed = ctx.IsKeyPressed(Key.J);
        var leftDown = ctx.IsKeyDown(Key.J);
        var rightPressed = ctx.IsKeyPressed(Key.H);

        if (_build.IsActive)
        {
            if (leftPressed)
            {
                _build.TryPlaceAtGround(_arena, ground, UnitTeam.Player);
            }

            if (rightPressed)
            {
                _build.Cancel();
            }

            return;
        }

        _selection.Update(_units, p => ScreenToGround(ctx.Scene.Camera, ctx, p), leftPressed, leftDown, rightPressed, mouse);
    }

    private static Vector2 PivotScreen(SilkTwoDGameContext ctx) =>
        new(ctx.Width * 0.5f, ctx.Height * 0.5f);

    private static Vector3 ScreenToGround(TwoDCamera camera, SilkTwoDGameContext ctx, Vector2 screen) =>
        camera.ScreenToWorld(screen.X, screen.Y);

    private void SpawnForces()
    {
        _units.Clear();
        SpawnSquad(UnitTeam.Player, _arena.SpawnPlayer, 8);
        SpawnSquad(UnitTeam.Enemy, _arena.SpawnEnemy, 6);
    }

    private void SpawnSquad(UnitTeam team, Vector3 anchor, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var offset = new Vector3((i % 4 - 1.5f) * 0.9f, 0f, (i / 4 - 0.5f) * 0.9f);
            _units.Add(new RtsUnit { Team = team, Position = anchor + offset });
        }
    }

    private void TickUnits(float dt)
    {
        foreach (var unit in _units)
        {
            unit.Tick(_arena, dt);
        }

        _enemyPulse += dt;
        if (_enemyPulse < 3f)
        {
            return;
        }

        _enemyPulse = 0f;
        var playerUnits = _units.Where(u => u.Team == UnitTeam.Player).ToList();
        if (playerUnits.Count == 0)
        {
            return;
        }

        var target = playerUnits[Random.Shared.Next(playerUnits.Count)].Position;
        foreach (var enemy in _units.Where(u => u.Team == UnitTeam.Enemy))
        {
            enemy.MoveTarget = target + new Vector3(Random.Shared.NextSingle() - 0.5f, 0f, Random.Shared.NextSingle() - 0.5f);
        }
    }

    private void TickProduction(float dt)
    {
        _spawnPulse += dt;
        if (_spawnPulse < 4f)
        {
            return;
        }

        _spawnPulse = 0f;
        TrySpawnFromBarracks(UnitTeam.Player);
        TrySpawnFromBarracks(UnitTeam.Enemy);
    }

    private void TrySpawnFromBarracks(UnitTeam team)
    {
        foreach (var b in _arena.Buildings)
        {
            if (b.Team != team || b.Type is not (RtsBuildingType.Barracks or RtsBuildingType.WarFactory))
            {
                continue;
            }

            var spawn = b.WorldCenter(_arena) + new Vector3(Random.Shared.NextSingle() - 0.5f, 0f, Random.Shared.NextSingle() - 0.5f);
            if (!_arena.IsBlocked(spawn.X, spawn.Z))
            {
                _units.Add(new RtsUnit { Team = team, Position = spawn });
            }
        }
    }

    private void BuildTerrain(TwoDScene scene)
    {
        for (var z = 0u; z < _arena.Walls.Height; z++)
        for (var x = 0u; x < _arena.Walls.Width; x++)
        {
            var minX = x * RtsArena.CellSize;
            var minZ = z * RtsArena.CellSize;
            if (_arena.Tiberium[x, z, 0] != 0)
            {
                scene.AddPlatform(minX, minZ, minX + 1f, minZ + 1f, Tiberium);
                continue;
            }

            if (_arena.Walls[x, z, 0] == 0)
            {
                if ((x + z) % 5 == 0)
                {
                    scene.AddPlatform(minX, minZ, minX + 0.95f, minZ + 0.95f, SandDark);
                }

                continue;
            }

            var color = x is >= 16 and <= 22 && z is >= 16 and <= 22 ? Water : WallRock;
            scene.AddPlatform(minX, minZ, minX + 1f, minZ + 1f, color);
        }
    }

    private void DrawBuildings(TwoDScene scene)
    {
        foreach (var b in _arena.Buildings)
        {
            var center = b.WorldCenter(_arena);
            var w = b.Width * RtsArena.CellSize * 0.9f;
            var h = b.Height * RtsArena.CellSize * 0.9f;
            var color = b.Team == UnitTeam.Player ? AlliedBuilding : SovietBuilding;
            scene.AddPlatform(center.X - w * 0.5f, center.Z - h * 0.5f, center.X + w * 0.5f, center.Z + h * 0.5f, color);
        }
    }

    private void DrawUnits(TwoDScene scene)
    {
        foreach (var marker in _dynamicMarkers)
        {
            scene.StaticPolygons.Remove(marker);
        }

        _dynamicMarkers.Clear();

        foreach (var unit in _units)
        {
            var color = unit.Team == UnitTeam.Player ? AlliedUnit : SovietUnit;
            if (unit.Selected)
            {
                _dynamicMarkers.Add(DenseGridPlatforms.AddSquareMarker(
                    scene,
                    unit.Position,
                    RtsUnit.Radius * 1.4f,
                    new Rgba32(255, 255, 255, 120),
                    sortKey: 900));
            }

            _dynamicMarkers.Add(DenseGridPlatforms.AddSquareMarker(scene, unit.Position, RtsUnit.Radius, color));

            if (unit.MoveTarget is { } target)
            {
                DrawOrderLine(scene, unit.Position, target);
            }
        }
    }

    private static void DrawOrderLine(TwoDScene scene, Vector3 from, Vector3 to)
    {
        var midZ = (from.Z + to.Z) * 0.5f;
        var thickness = 0.08f;
        scene.StaticPolygons.Add(new TwoDStaticPolygon(
            TwoDScenePrimitives.Rectangle(
                MathF.Min(from.X, to.X),
                midZ - thickness,
                MathF.Max(from.X, to.X),
                midZ + thickness),
            OrderLine)
        {
            DrawFilled = true,
            SortKey = 500,
        });
    }

    private void DrawHud(SilkTwoDGameContext ctx)
    {
        var scene = ctx.Scene;
        scene.Hud.Elements.Clear();
        scene.Hud.AddText("RTS Lite TwoD — orthographic top-down", 12, 12, 2f, HudText);
        scene.Hud.AddText("WASD pan  +/- zoom  |  1-5 build  B cancel", 12, 36, 2f, HudText);
        scene.Hud.AddText("J tap select  J hold box  H move order (screen center pivot)", 12, 60, 2f, HudText);
        var selected = _units.Count(u => u.Team == UnitTeam.Player && u.Selected);
        scene.Hud.AddText($"units {_units.Count}  selected {selected}", 12, 84, 2f, HudText);
        if (_build.ActiveType is { } t)
        {
            scene.Hud.AddText($"placing {t.Label()}", 12, 108, 2f, HudText);
        }
    }
}
