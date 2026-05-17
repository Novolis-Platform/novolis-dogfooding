using System.Drawing;
using System.Numerics;
using Novolis.Raylib.Game;
using Novolis.Raylib.Interact;
using Novolis.Simulation.View;
using Input = Novolis.Raylib.Interact.Input;
using RayCamera = Novolis.Raylib.Rendering.Camera;

namespace RtsLite.Game;

internal sealed class RtsLiteGame
{
    private static readonly Color Background = Color.FromArgb(255, 24, 30, 38);
    private static readonly Color WallColor = Color.FromArgb(255, 70, 85, 110);
    private static readonly Color FloorColor = Color.FromArgb(255, 40, 48, 58);
    private static readonly Color PlayerUnit = Color.FromArgb(255, 120, 220, 255);
    private static readonly Color EnemyUnit = Color.FromArgb(255, 220, 80, 80);
    private static readonly Color OrderLine = Color.FromArgb(180, 180, 220, 120);
    private static readonly Color HudText = Color.FromArgb(255, 210, 220, 235);

    private readonly DiagnosticsOverlay _diagnostics = new();
    private readonly OrbitCameraRig _camera = new();
    private readonly RtsSelection _selection = new();
    private readonly List<RtsUnit> _units = [];

    private RtsArena _arena = null!;
    private float _enemyPulse;

    public void Initialize(RayGameContext ctx)
    {
        _arena = RtsArena.Create();
        SpawnForces();
        _camera.SnapTarget(_arena.CellCenter(RtsArena.GridSize / 2, RtsArena.GridSize / 2));
        _camera.Distance = 22f;
        _camera.MinDistance = 10f;
        _camera.MaxDistance = 40f;
        _camera.Pitch = 0.9f;
    }

    public void Update(RayGameContext ctx)
    {
        UpdateCamera(ctx);
        var pose = _camera.BuildViewPose(ctx.DeltaSeconds);
        HandleSelection(ctx, pose);
        TickUnits(ctx.DeltaSeconds);
        _diagnostics.ToggleIfKeyPressed(ctx);

        ctx.Clear(Background);
        var camera = RayCamera.Perspective(pose.Position, pose.Target, pose.Up, pose.FieldOfViewDegrees);
        ctx.BeginWorld(camera);
        DrawArena(ctx);
        DrawUnits(ctx);
        ctx.EndWorld();

        _selection.DrawDragRect(ctx);
        RtsMinimap.Draw(ctx, _arena, _units);
        ctx.Text("Drag select  |  Right-click move  |  Wheel zoom  |  F3 diag", 16, 16, 18, HudText);
        _diagnostics.Draw(ctx, (_, lines) =>
        {
            var selected = _units.Count(u => u.Team == UnitTeam.Player && u.Selected);
            lines.Add($"units {_units.Count}  selected {selected}");
        });
    }

    private void SpawnForces()
    {
        _units.Clear();
        var center = _arena.CellCenter(RtsArena.GridSize / 2, RtsArena.GridSize / 2);
        for (var i = 0; i < 6; i++)
        {
            var offset = new Vector3((i % 3 - 1) * 1.2f, 0f, (i / 3 - 0.5f) * 1.2f);
            _units.Add(new RtsUnit { Team = UnitTeam.Player, Position = _arena.SpawnPlayer + offset });
        }

        for (var i = 0; i < 5; i++)
        {
            var offset = new Vector3((i - 2) * 0.9f, 0f, 0f);
            _units.Add(new RtsUnit { Team = UnitTeam.Enemy, Position = _arena.SpawnEnemy + offset });
        }

        _camera.Target = center;
    }

    private void UpdateCamera(RayGameContext ctx)
    {
        const float sensitivity = 0.003f;
        var delta = ctx.MouseDelta;
        _camera.AddLookDelta(-delta.X * sensitivity, -delta.Y * sensitivity);
        _camera.AdjustDistance(Input.GetMouseWheelMove() * -1.2f);

        if (ctx.IsKeyDown(KeyboardKey.W))
            _camera.Target += _cameraForwardXZ() * (6f * ctx.DeltaSeconds);
        if (ctx.IsKeyDown(KeyboardKey.S))
            _camera.Target -= _cameraForwardXZ() * (6f * ctx.DeltaSeconds);
        if (ctx.IsKeyDown(KeyboardKey.A))
            _camera.Target -= _cameraRightXZ() * (6f * ctx.DeltaSeconds);
        if (ctx.IsKeyDown(KeyboardKey.D))
            _camera.Target += _cameraRightXZ() * (6f * ctx.DeltaSeconds);
    }

    private Vector3 _cameraForwardXZ()
    {
        var f = new Vector3(MathF.Sin(_camera.Yaw), 0f, MathF.Cos(_camera.Yaw));
        return Vector3.Normalize(f);
    }

    private Vector3 _cameraRightXZ()
    {
        var r = new Vector3(MathF.Cos(_camera.Yaw), 0f, -MathF.Sin(_camera.Yaw));
        return Vector3.Normalize(r);
    }

    private void HandleSelection(RayGameContext ctx, ViewPose pose)
    {
        var mouse = Input.GetMousePosition();
        _selection.Update(
            _units,
            p => ScreenToGround(p, ctx, pose),
            ctx.IsMousePressed(MouseButton.Left),
            ctx.IsMouseDown(MouseButton.Left),
            ctx.IsMousePressed(MouseButton.Right),
            mouse);
    }

    private void TickUnits(float dt)
    {
        foreach (var unit in _units)
            unit.Tick(_arena, dt);

        _enemyPulse += dt;
        if (_enemyPulse < 2.5f)
            return;

        _enemyPulse = 0f;
        var playerUnits = _units.Where(u => u.Team == UnitTeam.Player).ToList();
        if (playerUnits.Count == 0)
            return;

        var target = playerUnits[Random.Shared.Next(playerUnits.Count)].Position;
        foreach (var enemy in _units.Where(u => u.Team == UnitTeam.Enemy))
            enemy.MoveTarget = target + new Vector3(Random.Shared.NextSingle() - 0.5f, 0f, Random.Shared.NextSingle() - 0.5f);
    }

    private static Vector3 ScreenToGround(Vector2 screen, RayGameContext ctx, ViewPose pose)
    {
        var nx = (screen.X / ctx.Width - 0.5f) * 2f;
        var ny = (0.5f - screen.Y / ctx.Height) * 2f;
        var aspect = (float)ctx.Width / Math.Max(ctx.Height, 1);
        var forward = Vector3.Normalize(pose.Target - pose.Position);
        var right = Vector3.Normalize(Vector3.Cross(forward, pose.Up));
        var up = Vector3.Normalize(Vector3.Cross(right, forward));
        var fovTan = MathF.Tan(pose.FieldOfViewDegrees * MathF.PI / 360f);
        var dir = Vector3.Normalize(forward + right * (nx * fovTan * aspect) + up * (ny * fovTan));

        var origin = pose.Position;
        if (MathF.Abs(dir.Y) < 1e-5f)
            return new Vector3(origin.X, 0f, origin.Z);

        var t = -origin.Y / dir.Y;
        if (t < 0f)
            t = 40f;
        var hit = origin + dir * t;
        return new Vector3(hit.X, 0f, hit.Z);
    }

    private void DrawArena(RayGameContext ctx)
    {
        ctx.DrawPlane(_arena.CellCenter(RtsArena.GridSize / 2, RtsArena.GridSize / 2), new Vector2(RtsArena.GridSize, RtsArena.GridSize), FloorColor);
        for (var z = 0u; z < _arena.Walls.Height; z++)
        for (var x = 0u; x < _arena.Walls.Width; x++)
        {
            if (_arena.Walls[x, z, 0] == 0)
                continue;

            var c = _arena.CellCenter(x, z);
            ctx.DrawShipBox(c + new Vector3(0f, 0.4f, 0f), new Vector3(RtsArena.CellSize, 0.8f, RtsArena.CellSize), WallColor);
        }
    }

    private void DrawUnits(RayGameContext ctx)
    {
        foreach (var unit in _units)
        {
            var color = unit.Team == UnitTeam.Player ? PlayerUnit : EnemyUnit;
            if (unit.Selected)
                ctx.DrawGlowSphereWires(unit.Position + new Vector3(0f, 0.35f, 0f), RtsUnit.Radius + 0.12f, Color.FromArgb(255, 255, 255, 180));

            ctx.DrawGlowSphere(unit.Position + new Vector3(0f, 0.35f, 0f), RtsUnit.Radius, color);

            if (unit.MoveTarget is { } target)
                ctx.DrawBolt(unit.Position + new Vector3(0f, 0.35f, 0f), target + new Vector3(0f, 0.35f, 0f), OrderLine);
        }
    }
}
