using System.Drawing;
using System.Numerics;
using Novolis.Raylib.Game;
using Novolis.Raylib.Rendering;

namespace XFighter.Game;

internal sealed class HFighter
{
    private static readonly Color Hull = Color.FromArgb(255, 90, 95, 110);
    private static readonly Color Wing = Color.FromArgb(255, 70, 75, 90);
    private static readonly Color CockpitGlass = Color.FromArgb(255, 40, 180, 220);
    private static readonly Color EngineGlow = Color.FromArgb(255, 255, 90, 40);

    public Vector3 Position;
    public Vector3 Velocity;
    public float Health = 1f;
    public float WeavePhase;
    public bool Active;

    public float HitRadius => 2.2f;

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

        WeavePhase += dt * 2.4f;
        var toPlayer = playerPos - Position;
        var dir = Vector3.Normalize(toPlayer);
        var right = Vector3.Normalize(Vector3.Cross(dir, new Vector3(0, 1, 0)));
        var weave = right * MathF.Sin(WeavePhase) * 0.35f + new Vector3(0, MathF.Cos(WeavePhase * 0.7f) * 0.15f, 0);
        Velocity = Vector3.Normalize(dir + weave) * 14f;
        Position += Velocity * dt;
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
        drawBox(center, new Vector3(1.2f, 0.9f, 1.8f), Hull);
        drawBox(center + right * 2.8f, new Vector3(0.35f, 0.2f, 2.4f), Wing);
        drawBox(center - right * 2.8f, new Vector3(0.35f, 0.2f, 2.4f), Wing);
        drawBox(center + forward * 0.6f + up * 0.2f, new Vector3(0.8f, 0.5f, 0.8f), CockpitGlass);
        drawSphere(center - forward * 1.4f + right * 1.1f, 0.35f, EngineGlow);
        drawSphere(center - forward * 1.4f - right * 1.1f, 0.35f, EngineGlow);
        drawWires(center, new Vector3(6.5f, 2.5f, 4f), Color.FromArgb(180, 30, 30, 40));
    }
}
