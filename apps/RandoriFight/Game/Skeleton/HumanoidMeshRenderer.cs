using System.Drawing;
using System.Numerics;
using Novolis.Raylib.Game;

namespace RandoriFight.Game.Skeleton;

/// <summary>Standard-rig skin mesh + optional skeleton overlay (Unity / metaball reference).</summary>
internal static class HumanoidMeshRenderer
{
    private static readonly Color Blade = Color.FromArgb(255, 198, 208, 222);
    private static readonly Color BladeEdge = Color.FromArgb(255, 240, 246, 255);
    private static readonly Color Tsuba = Color.FromArgb(255, 58, 48, 42);
    private static readonly Color Sageo = Color.FromArgb(255, 168, 42, 48);
    private static readonly Color CutTrail = Color.FromArgb(100, 255, 220, 160);

    public static void Draw(
        RayGameContext ctx,
        SkeletonFrame skeleton,
        bool isPlayer,
        bool showSkeleton,
        bool hitFlash,
        bool strikeWindow,
        bool showCutTrail)
    {
        var skin = StandardRigBuilder.BuildSegments(skeleton, isPlayer);
        MetaballMesh.DrawSkin(ctx, skin);

        if (showSkeleton)
        {
            var bones = StandardRigBuilder.BuildSkeletonBones(skeleton);
            MetaballMesh.DrawSkeletonOverlay(ctx, bones);
        }

        if (hitFlash)
            ctx.DrawGlowSphere(skeleton[HumanoidBoneId.Head], 0.12f, Color.FromArgb(255, 255, 150, 100));

        DrawKatana(ctx, skeleton, strikeWindow, showCutTrail);
    }

    private static void DrawKatana(RayGameContext ctx, SkeletonFrame s, bool strikeWindow, bool showCutTrail)
    {
        var root = s.BladeRoot;
        var tip = s.BladeTip;
        ctx.DrawBolt(root, tip, Blade);
        var tsuba = Vector3.Lerp(root, s[HumanoidBoneId.RightHand], 0.32f);
        ctx.DrawGlowSphere(tsuba, 0.05f, Tsuba);
        ctx.DrawGlowSphere(root, 0.038f, Sageo);
        ctx.DrawGlowSphere(tip, 0.034f, BladeEdge);

        if (strikeWindow)
            ctx.DrawGlowSphereWires(tip, 0.2f, CutTrail);

        if (showCutTrail)
            ctx.DrawBolt(Vector3.Lerp(root, tip, 0.55f), tip, CutTrail);
    }
}
