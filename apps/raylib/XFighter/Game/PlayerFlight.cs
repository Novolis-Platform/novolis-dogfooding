using System.Numerics;
using Novolis.Raylib;
using Novolis.Raylib.Game;
using Novolis.Raylib.Rendering;

namespace XFighter.Game;

internal sealed class PlayerFlight
{
    public float Yaw;
    public float Pitch;
    public float Speed = 22f;
    public Vector3 Position = Vector3.Zero;

    public Vector3 Forward => RaylibVector3.ForwardFromYawPitch(Yaw, Pitch);

    public void Update(RayGameContext ctx)
    {
        var dt = ctx.DeltaSeconds;
        var delta = ctx.MouseDelta;
        Yaw += delta.X * 0.0022f;
        Pitch = Math.Clamp(Pitch - delta.Y * 0.0022f, -1.1f, 1.1f);

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
        return Camera.Perspective(eye, target, new Vector3(0, 1, 0), 72f);
    }
}
