using System.Drawing;
using System.Numerics;
using Novolis.Raylib.Game;

namespace RandoriFight.Game;

internal static class FighterRenderer
{
    private static readonly Color Hakama = Color.FromArgb(255, 28, 32, 48);
    private static readonly Color HakamaFold = Color.FromArgb(255, 18, 22, 34);
    private static readonly Color Gi = Color.FromArgb(255, 235, 228, 210);
    private static readonly Color GiAi = Color.FromArgb(255, 210, 218, 232);
    private static readonly Color Skin = Color.FromArgb(255, 192, 148, 112);
    private static readonly Color Blade = Color.FromArgb(255, 198, 208, 222);
    private static readonly Color BladeEdge = Color.FromArgb(255, 235, 242, 255);
    private static readonly Color Tsuba = Color.FromArgb(255, 58, 48, 42);
    private static readonly Color Sageo = Color.FromArgb(255, 168, 42, 48);
    private static readonly Color HitFlash = Color.FromArgb(255, 255, 140, 100);
    private static readonly Color CutTrail = Color.FromArgb(100, 255, 220, 160);

    public static void Draw(RayGameContext ctx, Fighter fighter, bool isPlayer)
    {
        var face = fighter.Facing;
        var pose = fighter.CurrentPose;
        var root = fighter.WorldPosition;
        var gi = isPlayer ? Gi : GiAi;
        var flash = fighter.State == FighterState.HitStun;

        DrawSegment(ctx, root, face, pose.LeftFoot, pose.Hips, 0.1f, HakamaFold);
        DrawSegment(ctx, root, face, pose.RightFoot, pose.Hips, 0.1f, HakamaFold);
        DrawSegment(ctx, root, face, pose.Hips, pose.Chest, 0.15f, gi);
        DrawSegment(ctx, root, face, pose.Chest, pose.Head, 0.11f, flash ? HitFlash : Skin);
        DrawSegment(ctx, root, face, pose.LeftHand, pose.RightHand, 0.05f, Skin);
        DrawKatana(ctx, root, face, pose, fighter);

        if (fighter.IsAttacking && fighter.MoveNormalizedTime is > 0.32f and < 0.68f)
            DrawCutTrail(ctx, root, face, pose);
    }

    private static void DrawKatana(RayGameContext ctx, Vector3 root, int face, KatanaPose pose, Fighter fighter)
    {
        var a = Local(root, face, pose.BladeRoot);
        var b = Local(root, face, pose.BladeTip);
        var tsuba = Local(root, face, Vector3.Lerp(pose.BladeRoot, pose.RightHand, 0.35f));

        ctx.DrawLaserBolt(a, b, Blade);
        ctx.DrawGlowSphere(tsuba, 0.055f, Tsuba);
        ctx.DrawGlowSphere(a, 0.045f, Sageo);
        ctx.DrawGlowSphere(b, 0.04f, BladeEdge);

        if (fighter.IsStrikeWindow)
            ctx.DrawGlowSphereWires(b, 0.22f, CutTrail);
    }

    private static void DrawCutTrail(RayGameContext ctx, Vector3 root, int face, KatanaPose pose)
    {
        var tip = Local(root, face, pose.BladeTip);
        var mid = Local(root, face, Vector3.Lerp(pose.BladeRoot, pose.BladeTip, 0.55f));
        ctx.DrawBolt(mid, tip, CutTrail);
    }

    private static void DrawSegment(
        RayGameContext ctx,
        Vector3 root,
        int face,
        Vector3 localA,
        Vector3 localB,
        float radius,
        Color color)
    {
        var a = Local(root, face, localA);
        var b = Local(root, face, localB);
        ctx.DrawLaserBolt(a, b, color);
        ctx.DrawGlowSphere(a, radius, color);
        ctx.DrawGlowSphere(b, radius * 0.9f, color);
    }

    private static Vector3 Local(Vector3 root, int face, Vector3 local) =>
        root + new Vector3(local.X * face, local.Y, local.Z);
}
