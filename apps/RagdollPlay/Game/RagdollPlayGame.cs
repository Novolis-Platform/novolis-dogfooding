using System.Drawing;
using System.Numerics;
using Novolis.Raylib.Game;
using Novolis.Raylib.Interact;
using Novolis.Simulation.View;
using Input = Novolis.Raylib.Interact.Input;
using RayCamera = Novolis.Raylib.Rendering.Camera;

namespace RagdollPlay.Game;

internal sealed class RagdollPlayGame
{
    private static readonly Color Background = Color.FromArgb(255, 22, 26, 34);
    private static readonly Color WallWire = Color.FromArgb(255, 80, 95, 120);
    private static readonly Color HudText = Color.FromArgb(255, 210, 220, 235);

    private readonly DiagnosticsOverlay _diagnostics = new();
    private readonly OrbitCameraRig _camera = new();

    private PlayRoom _room = null!;
    private RagdollBody _ragdoll = null!;

    public void Initialize(RayGameContext ctx)
    {
        _room = PlayRoom.Create();
        _ragdoll = new RagdollBody();
        ResetRagdoll();
        _camera.SnapTarget(_room.RoomCenter);
        _camera.Distance = 9f;
        _camera.MinDistance = 4f;
        _camera.MaxDistance = 18f;
        _camera.Yaw = 0.6f;
        _camera.Pitch = 0.35f;
    }

    public void Update(RayGameContext ctx)
    {
        UpdateCamera(ctx);
        if (ctx.IsKeyPressed(KeyboardKey.R))
            ResetRagdoll();

        var pose = _camera.BuildViewPose(ctx.DeltaSeconds);
        TryImpulseFromClick(ctx, pose);
        _diagnostics.ToggleIfKeyPressed(ctx);
        _ragdoll.Step(_room.CollisionWorld, ctx.DeltaSeconds);

        ctx.Clear(Background);
        var camera = RayCamera.Perspective(pose.Position, pose.Target, pose.Up, pose.FieldOfViewDegrees);
        ctx.BeginWorld(camera);
        DrawRoom(ctx);
        DrawRagdoll(ctx);
        ctx.EndWorld();

        ctx.Text("Click ragdoll to shove  |  R reset  |  F3 diag", 16, 16, 18, HudText);
        _diagnostics.Draw(ctx, (_, lines) =>
        {
            lines.Add($"spheres {_ragdoll.Spheres.Count}  joints {_ragdoll.Joints.Count}");
            lines.Add($"joint corrections {_ragdoll.LastJointCorrections}");
        });
    }

    private void ResetRagdoll()
    {
        var spawn = _room.RoomCenter + new Vector3(0f, 0.3f, 0f);
        _ragdoll.SpawnStanding(spawn, _room);
    }

    private void UpdateCamera(RayGameContext ctx)
    {
        const float sensitivity = 0.004f;
        var delta = ctx.MouseDelta;
        _camera.AddLookDelta(-delta.X * sensitivity, -delta.Y * sensitivity);
        _camera.AdjustDistance(Input.GetMouseWheelMove() * -0.8f);
    }

    private void TryImpulseFromClick(RayGameContext ctx, ViewPose pose)
    {
        if (!ctx.IsMousePressed(MouseButton.Left))
            return;

        var mouse = Input.GetMousePosition();
        var nx = (mouse.X / ctx.Width - 0.5f) * 2f;
        var ny = (0.5f - mouse.Y / ctx.Height) * 2f;
        var aspect = (float)ctx.Width / Math.Max(ctx.Height, 1);
        var (origin, direction) = BuildPickRay(pose, nx, ny, aspect);

        if (!PainterDollRenderer.TryPickBone(origin, direction, _ragdoll.Spheres, 0.12f, out var best, out _))
            return;

        var impulseDir = Vector3.Normalize(direction + new Vector3(0f, 0.35f, 0f));
        _ragdoll.ApplyImpulse(best, impulseDir * 6f);
    }

    private static (Vector3 Origin, Vector3 Direction) BuildPickRay(ViewPose pose, float nx, float ny, float aspect)
    {
        var forward = Vector3.Normalize(pose.Target - pose.Position);
        var right = Vector3.Normalize(Vector3.Cross(forward, pose.Up));
        var up = Vector3.Normalize(Vector3.Cross(right, forward));
        var fovTan = MathF.Tan(pose.FieldOfViewDegrees * MathF.PI / 360f);
        var dir = Vector3.Normalize(forward + right * (nx * fovTan * aspect) + up * (ny * fovTan));
        return (pose.Position, dir);
    }

    private void DrawRoom(RayGameContext ctx)
    {
        var h = PlayRoom.WallHeight * 0.5f;
        for (var y = 0u; y < PlayRoom.GridSize; y++)
        for (var x = 0u; x < PlayRoom.GridSize; x++)
        {
            if (x != 0 && y != 0 && x != PlayRoom.GridSize - 1 && y != PlayRoom.GridSize - 1)
                continue;

            var cx = (x + 0.5f) * PlayRoom.CellSize;
            var cz = (y + 0.5f) * PlayRoom.CellSize;
            ctx.DrawShipWires(
                new Vector3(cx, h, cz),
                new Vector3(PlayRoom.CellSize, PlayRoom.WallHeight, PlayRoom.CellSize),
                WallWire);
        }
    }

    private void DrawRagdoll(RayGameContext ctx) =>
        PainterDollRenderer.Draw(ctx, _ragdoll.Spheres);
}

internal static class RaySphere
{
    public static bool TryHit(Vector3 origin, Vector3 direction, Vector3 center, float radius, out float t)
    {
        var oc = origin - center;
        var a = Vector3.Dot(direction, direction);
        var b = 2f * Vector3.Dot(oc, direction);
        var c = Vector3.Dot(oc, oc) - radius * radius;
        var disc = b * b - 4f * a * c;
        if (disc < 0f || MathF.Abs(a) < 1e-8f)
        {
            t = -1f;
            return false;
        }

        t = (-b - MathF.Sqrt(disc)) / (2f * a);
        return t >= 0f;
    }
}
