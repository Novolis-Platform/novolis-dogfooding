using System.Drawing;
using System.Numerics;
using Novolis.Raylib.Game;
using Novolis.Raylib.Rendering;

namespace XFighter.Game;

internal sealed class HFighter
{
    private static readonly Color BallHull = Color.FromArgb(255, 18, 18, 22);
    private static readonly Color SolarPanel = Color.FromArgb(255, 55, 58, 65);
    private static readonly Color PanelFrame = Color.FromArgb(255, 35, 38, 45);
    private static readonly Color EngineGlow = Color.FromArgb(255, 255, 50, 35);

    public Vector3 Position;
    public Vector3 Velocity;
    public float Health = 1f;
    public float WeavePhase;
    public float FireCooldown;
    public bool Active;

    public float HitRadius => 2.4f;

    public void Spawn(Vector3 playerPos, Vector3 playerForward, Random rng)
    {
        Active = true;
        Health = 1f;
        WeavePhase = (float)rng.NextDouble() * MathF.Tau;

        var yaw = MathF.Atan2(playerForward.X, -playerForward.Z) + (float)(rng.NextDouble() * 1.2 - 0.6);
        var dist = 55f + (float)rng.NextDouble() * 35f;
        Position = playerPos + new Vector3(MathF.Sin(yaw) * dist, (float)(rng.NextDouble() * 6 - 3), -MathF.Cos(yaw) * dist);
        Velocity = Vector3.Zero;
    }

    public void Update(float dt, Vector3 playerPos)
    {
        if (!Active)
            return;

        FireCooldown = Math.Max(0, FireCooldown - dt);
        WeavePhase += dt * 2.4f;
        var toPlayer = playerPos - Position;
        var dir = Vector3.Normalize(toPlayer);
        var right = Vector3.Normalize(Vector3.Cross(dir, new Vector3(0, 1, 0)));
        var weave = right * MathF.Sin(WeavePhase) * 0.35f + new Vector3(0, MathF.Cos(WeavePhase * 0.7f) * 0.15f, 0);
        Velocity = Vector3.Normalize(dir + weave) * 14f;
        Position += Velocity * dt;
    }

    public bool TryFire(Vector3 playerPos, out Vector3 origin, out Vector3 velocity)
    {
        origin = default;
        velocity = default;
        if (!Active || FireCooldown > 0)
            return false;

        var toPlayer = playerPos - Position;
        if (toPlayer.LengthSquared() > 70f * 70f || toPlayer.LengthSquared() < 12f * 12f)
            return false;

        FireCooldown = 0.85f + (float)Random.Shared.NextDouble() * 0.5f;
        origin = Position + Vector3.Normalize(toPlayer) * 1.2f;
        velocity = Vector3.Normalize(toPlayer) * 95f;
        return true;
    }

    public void Draw(RayGameContext ctx) => DrawInternal(ctx.DrawShipBox, ctx.DrawShipWires, ctx.DrawGlowSphere);

    public void DrawHarness() =>
        DrawInternal(World.DrawCubeV, World.DrawCubeWiresV, World.DrawSphere);

    private void DrawInternal(
        Action<Vector3, Vector3, Color> drawBox,
        Action<Vector3, Vector3, Color> drawWires,
        Action<Vector3, float, Color> drawSphere)
    {
        if (!Active)
            return;

        var forward = Vector3.Normalize(Velocity);
        if (forward.X == 0 && forward.Y == 0 && forward.Z == 0)
            forward = new Vector3(0, 0, 1);

        var right = Vector3.Normalize(Vector3.Cross(forward, new Vector3(0, 1, 0)));
        var up = Vector3.Normalize(Vector3.Cross(right, forward));

        var center = Position;
        drawSphere(center, 1.05f, BallHull);
        drawBox(center + right * 3.6f, new Vector3(0.12f, 2.8f, 3.6f), SolarPanel);
        drawBox(center - right * 3.6f, new Vector3(0.12f, 2.8f, 3.6f), SolarPanel);
        drawBox(center + right * 3.6f, new Vector3(0.18f, 2.9f, 3.7f), PanelFrame);
        drawBox(center - right * 3.6f, new Vector3(0.18f, 2.9f, 3.7f), PanelFrame);
        drawSphere(center - forward * 0.9f, 0.28f, EngineGlow);
        drawWires(center, new Vector3(7.8f, 3.2f, 4.2f), Color.FromArgb(140, 25, 25, 30));
    }
}
