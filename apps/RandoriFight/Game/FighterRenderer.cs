using System.Drawing;
using System.Numerics;
using Novolis.Raylib.Game;

namespace RandoriFight.Game;

internal static class FighterRenderer
{
    private static readonly Color Gi = Color.FromArgb(255, 228, 222, 198);
    private static readonly Color GiShadow = Color.FromArgb(255, 168, 158, 132);
    private static readonly Color Belt = Color.FromArgb(255, 42, 38, 48);
    private static readonly Color Skin = Color.FromArgb(255, 198, 150, 118);
    private static readonly Color SkinDark = Color.FromArgb(255, 138, 98, 72);
    private static readonly Color AiGi = Color.FromArgb(255, 198, 210, 228);
    private static readonly Color AiShadow = Color.FromArgb(255, 118, 138, 168);
    private static readonly Color HitFlash = Color.FromArgb(255, 255, 120, 90);

    public static void Draw(RayGameContext ctx, Fighter fighter, bool isPlayer)
    {
        var root = fighter.WorldPosition;
        var face = fighter.Facing;
        var phase = fighter.AttackPhaseT();
        var walk = fighter.MoveAnimPhase;
        var gi = isPlayer ? Gi : AiGi;
        var shadow = isPlayer ? GiShadow : AiShadow;

        var bob = fighter.State == FighterState.Walk ? MathF.Sin(walk) * 0.04f : 0f;
        root.Y += bob;

        GetPose(fighter.State, phase, walk, out var spineLean, out var armL, out var armR, out var legL, out var legR);

        DrawLimb(ctx, root, face, new Vector3(0f, 0.45f, 0f), new Vector3(0f, 0.95f, 0f), 0.16f, gi);
        DrawLimb(ctx, root, face, new Vector3(0f, 0.95f, 0f), new Vector3(spineLean * 0.15f, 1.45f, 0f), 0.14f, gi);
        DrawLimb(ctx, root, face, new Vector3(-0.18f * face, 1.35f, 0f), new Vector3(-0.18f * face + armL.X, 1.35f + armL.Y, armL.Z), 0.09f, gi);
        DrawLimb(ctx, root, face, new Vector3(0.18f * face, 1.35f, 0f), new Vector3(0.18f * face + armR.X, 1.35f + armR.Y, armR.Z), 0.09f, gi);
        DrawLimb(ctx, root, face, new Vector3(-0.12f * face, 0.45f, 0f), new Vector3(-0.14f * face + legL.X, legL.Y, legL.Z), 0.11f, shadow);
        DrawLimb(ctx, root, face, new Vector3(0.12f * face, 0.45f, 0f), new Vector3(0.14f * face + legR.X, legR.Y, legR.Z), 0.11f, shadow);

        ctx.DrawGlowSphere(root + new Vector3(0f, 1.62f, 0f), 0.14f, fighter.State == FighterState.HitStun ? HitFlash : Skin);
        ctx.DrawGlowSphere(root + new Vector3(0f, 0.92f, 0f), 0.2f, Belt);

        if (fighter.IsAttacking && fighter.AttackPhaseT() > 0.2f)
        {
            var hit = fighter.AttackCenter();
            ctx.DrawGlowSphereWires(hit, fighter.State == FighterState.Kick ? 0.5f : 0.42f, Color.FromArgb(80, 255, 200, 120));
        }
    }

    private static void GetPose(
        FighterState state,
        float phase,
        float walk,
        out float spineLean,
        out Vector3 armL,
        out Vector3 armR,
        out Vector3 legL,
        out Vector3 legR)
    {
        spineLean = 0f;
        armL = new Vector3(-0.22f, -0.12f, 0f);
        armR = new Vector3(0.22f, -0.12f, 0f);
        legL = new Vector3(-0.08f, -0.02f, 0f);
        legR = new Vector3(0.08f, -0.02f, 0f);

        switch (state)
        {
            case FighterState.Walk:
                legL.Y = MathF.Sin(walk) * 0.18f;
                legR.Y = -MathF.Sin(walk) * 0.18f;
                armL.Y = -MathF.Sin(walk) * 0.1f;
                armR.Y = MathF.Sin(walk) * 0.1f;
                break;
            case FighterState.Punch:
                spineLean = 0.12f * phase;
                armR = new Vector3(0.55f * phase, 0.08f, 0f);
                armL = new Vector3(-0.28f, -0.2f, 0f);
                legR.X = 0.2f * phase;
                break;
            case FighterState.Kick:
                spineLean = -0.08f * phase;
                armL = new Vector3(-0.35f, 0.05f, 0f);
                armR = new Vector3(0.3f, 0.05f, 0f);
                legR = new Vector3(0.65f * phase, 0.35f * phase, 0f);
                legL.X = -0.15f;
                break;
            case FighterState.Block:
                armL = new Vector3(-0.18f, 0.22f, 0.12f);
                armR = new Vector3(0.18f, 0.22f, 0.12f);
                spineLean = -0.06f;
                break;
            case FighterState.HitStun:
                spineLean = -0.18f;
                armL = new Vector3(-0.35f, 0.1f, 0f);
                armR = new Vector3(0.35f, 0.1f, 0f);
                break;
            case FighterState.Ko:
                spineLean = -0.45f;
                armL = new Vector3(-0.5f, -0.05f, 0f);
                armR = new Vector3(0.5f, -0.05f, 0f);
                legL = new Vector3(-0.2f, -0.35f, 0f);
                legR = new Vector3(0.2f, -0.35f, 0f);
                break;
        }
    }

    private static void DrawLimb(
        RayGameContext ctx,
        Vector3 root,
        int face,
        Vector3 localA,
        Vector3 localB,
        float radius,
        Color color)
    {
        localA.X *= face;
        localB.X *= face;
        var a = root + localA;
        var b = root + localB;
        ctx.DrawLaserBolt(a, b, color);
        ctx.DrawGlowSphere(a, radius, SkinDark);
        ctx.DrawGlowSphere(b, radius * 0.92f, color);
    }
}
