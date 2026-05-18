using System.Numerics;
using Novolis.Raylib.Game;
using Novolis.Raylib.Interact;
using Novolis.Simulation.View;
using Input = Novolis.Raylib.Interact.Input;

namespace RtsLite.Game;

/// <summary>Fixed Red Alert–style diagonal RTS view: no orbit, pan + zoom only.</summary>
internal sealed class RtsClassicCamera
{
    /// <summary>Classic C&amp;C diagonal (camera south-east of map, looking north-west).</summary>
    public const float FixedYaw = MathF.PI * 0.75f;

    /// <summary>~52° elevation — traditional RTS tabletop angle.</summary>
    public const float FixedPitch = 0.92f;

    public Vector3 PanTarget { get; set; }
    public float Distance { get; set; } = 26f;
    public float MinDistance { get; set; } = 14f;
    public float MaxDistance { get; set; } = 42f;
    public float FieldOfViewDegrees { get; set; } = 42f;

    public void SnapTo(Vector3 worldPoint) => PanTarget = new Vector3(worldPoint.X, 0f, worldPoint.Z);

    public void Update(RayGameContext ctx)
    {
        var dt = ctx.DeltaSeconds;
        var panSpeed = 14f * (Distance / 26f);

        var forward = GroundForward();
        var right = GroundRight();

        if (ctx.IsKeyDown(KeyboardKey.W))
            PanTarget += forward * (panSpeed * dt);
        if (ctx.IsKeyDown(KeyboardKey.S))
            PanTarget -= forward * (panSpeed * dt);
        if (ctx.IsKeyDown(KeyboardKey.A))
            PanTarget -= right * (panSpeed * dt);
        if (ctx.IsKeyDown(KeyboardKey.D))
            PanTarget += right * (panSpeed * dt);

        if (ctx.IsMouseDown(MouseButton.Middle))
        {
            var delta = ctx.MouseDelta;
            var dragScale = 0.028f * (Distance / 26f);
            PanTarget -= right * (delta.X * dragScale);
            PanTarget += forward * (delta.Y * dragScale);
        }

        EdgeScroll(ctx, right, forward, panSpeed * dt);

        Distance = Math.Clamp(Distance + Input.GetMouseWheelMove() * -1.8f, MinDistance, MaxDistance);
        ClampPan(ctx);
    }

    public ViewPose BuildViewPose()
    {
        var cosP = MathF.Cos(FixedPitch);
        var sinP = MathF.Sin(FixedPitch);
        var offset = new Vector3(
            MathF.Sin(FixedYaw) * cosP * Distance,
            sinP * Distance,
            MathF.Cos(FixedYaw) * cosP * Distance);
        var eye = PanTarget + offset;
        return new ViewPose(eye, PanTarget, Vector3.UnitY, FieldOfViewDegrees);
    }

    public Vector3 ScreenToGround(Vector2 screen, int screenW, int screenH)
    {
        var pose = BuildViewPose();
        var nx = (screen.X / screenW - 0.5f) * 2f;
        var ny = (0.5f - screen.Y / screenH) * 2f;
        var aspect = (float)screenW / Math.Max(screenH, 1);
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
            t = 50f;
        var hit = origin + dir * t;
        return new Vector3(hit.X, 0f, hit.Z);
    }

    private static Vector3 GroundForward() =>
        Vector3.Normalize(new Vector3(-MathF.Sin(FixedYaw), 0f, -MathF.Cos(FixedYaw)));

    private static Vector3 GroundRight() =>
        Vector3.Normalize(new Vector3(MathF.Cos(FixedYaw), 0f, -MathF.Sin(FixedYaw)));

    private void EdgeScroll(RayGameContext ctx, Vector3 right, Vector3 forward, float step)
    {
        var mouse = Input.GetMousePosition();
        const int margin = 28;
        if (mouse.X < margin)
            PanTarget -= right * step;
        if (mouse.X > ctx.Width - margin)
            PanTarget += right * step;
        if (mouse.Y < margin)
            PanTarget += forward * step;
        if (mouse.Y > ctx.Height - margin)
            PanTarget -= forward * step;
    }

    private void ClampPan(RayGameContext ctx)
    {
        var half = RtsArena.GridSize * RtsArena.CellSize * 0.5f;
        var margin = 2f;
        PanTarget = new Vector3(
            Math.Clamp(PanTarget.X, margin, half * 2f - margin),
            0f,
            Math.Clamp(PanTarget.Z, margin, half * 2f - margin));
    }
}
