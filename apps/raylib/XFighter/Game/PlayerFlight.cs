using System.Numerics;
using Novolis.Raylib;
using Novolis.Raylib.Game;
using Novolis.Raylib.Rendering;

namespace XFighter.Game;

internal sealed class PlayerFlight
{
    public float Yaw;
    public float Pitch;
    public float Roll;
    public float Speed = 22f;
    public Vector3 Position = Vector3.Zero;

    public Vector3 Forward => RaylibVector3.ForwardFromYawPitch(Yaw, Pitch);

    public float Throttle01 => Math.Clamp((Speed - 6f) / 42f, 0f, 1f);

    public void Update(RayGameContext ctx)
    {
        var dt = ctx.DeltaSeconds;
        var delta = ctx.MouseDelta;
        Yaw += delta.X * 0.0022f;
        Pitch = Math.Clamp(Pitch - delta.Y * 0.0022f, -1.1f, 1.1f);

        if (ctx.IsKeyDown(Novolis.Raylib.Interact.KeyboardKey.A))
            Roll = Math.Min(Roll + 2.8f * dt, 0.75f);
        if (ctx.IsKeyDown(Novolis.Raylib.Interact.KeyboardKey.D))
            Roll = Math.Max(Roll - 2.8f * dt, -0.75f);
        Roll *= 1f - 3.5f * dt;

        if (ctx.IsKeyDown(Novolis.Raylib.Interact.KeyboardKey.W))
            Speed = Math.Min(Speed + 28f * dt, 48f);
        if (ctx.IsKeyDown(Novolis.Raylib.Interact.KeyboardKey.S))
            Speed = Math.Max(Speed - 22f * dt, 6f);

        Speed *= 1f - 0.35f * dt;
        Position += Forward * (Speed * dt);
    }

    public Camera BuildCamera()
    {
        var eye = Position + new Vector3(0, 0.35f, 0);
        var target = eye + Forward * 10f;
        var worldUp = Vector3.UnitY;
        var right = Vector3.Normalize(Vector3.Cross(Forward, worldUp));
        var up = Vector3.Normalize(Vector3.Cross(right, Forward));
        var rolledUp = Vector3.Normalize(up * MathF.Cos(Roll) + right * MathF.Sin(Roll));
        return Camera.Perspective(eye, target, rolledUp, 72f);
    }
}
