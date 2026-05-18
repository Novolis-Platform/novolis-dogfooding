using System.Drawing;
using System.Numerics;
using Novolis.Raylib.Game;

namespace RandoriFight.Game.Skeleton;

/// <summary>Blender-style posable mannequin: joint spheres, tapered limb bones, pelvis block.</summary>
internal static class PosableMannequinRenderer
{
    private static readonly Color Joint = Color.FromArgb(255, 48, 44, 52);
    private static readonly Color JointHi = Color.FromArgb(255, 72, 68, 78);
    private static readonly Color BoneWood = Color.FromArgb(255, 188, 152, 108);
    private static readonly Color BoneWoodHi = Color.FromArgb(255, 218, 188, 148);
    private static readonly Color BoneWire = Color.FromArgb(255, 58, 42, 28);
    private static readonly Color Torso = Color.FromArgb(255, 228, 220, 200);
    private static readonly Color TorsoAi = Color.FromArgb(255, 200, 212, 228);
    private static readonly Color HeadTone = Color.FromArgb(255, 210, 168, 128);
    private static readonly Color HandTone = Color.FromArgb(255, 192, 148, 112);
    private static readonly Color FootTone = Color.FromArgb(255, 38, 36, 44);
    private static readonly Color Blade = Color.FromArgb(255, 198, 208, 222);
    private static readonly Color BladeEdge = Color.FromArgb(255, 240, 246, 255);
    private static readonly Color Tsuba = Color.FromArgb(255, 58, 48, 42);
    private static readonly Color Sageo = Color.FromArgb(255, 168, 42, 48);
    private static readonly Color CutTrail = Color.FromArgb(100, 255, 220, 160);

    private static readonly (HumanoidBoneId A, HumanoidBoneId B)[] BonePairs =
    [
        (HumanoidBoneId.Pelvis, HumanoidBoneId.SpineLower),
        (HumanoidBoneId.SpineLower, HumanoidBoneId.SpineMid),
        (HumanoidBoneId.SpineMid, HumanoidBoneId.Chest),
        (HumanoidBoneId.Chest, HumanoidBoneId.Neck),
        (HumanoidBoneId.Neck, HumanoidBoneId.Head),
        (HumanoidBoneId.Pelvis, HumanoidBoneId.LeftHip),
        (HumanoidBoneId.LeftHip, HumanoidBoneId.LeftKnee),
        (HumanoidBoneId.LeftKnee, HumanoidBoneId.LeftAnkle),
        (HumanoidBoneId.LeftAnkle, HumanoidBoneId.LeftToe),
        (HumanoidBoneId.Pelvis, HumanoidBoneId.RightHip),
        (HumanoidBoneId.RightHip, HumanoidBoneId.RightKnee),
        (HumanoidBoneId.RightKnee, HumanoidBoneId.RightAnkle),
        (HumanoidBoneId.RightAnkle, HumanoidBoneId.RightToe),
        (HumanoidBoneId.Chest, HumanoidBoneId.LeftClavicle),
        (HumanoidBoneId.LeftClavicle, HumanoidBoneId.LeftShoulder),
        (HumanoidBoneId.LeftShoulder, HumanoidBoneId.LeftElbow),
        (HumanoidBoneId.LeftElbow, HumanoidBoneId.LeftWrist),
        (HumanoidBoneId.LeftWrist, HumanoidBoneId.LeftHand),
        (HumanoidBoneId.Chest, HumanoidBoneId.RightClavicle),
        (HumanoidBoneId.RightClavicle, HumanoidBoneId.RightShoulder),
        (HumanoidBoneId.RightShoulder, HumanoidBoneId.RightElbow),
        (HumanoidBoneId.RightElbow, HumanoidBoneId.RightWrist),
        (HumanoidBoneId.RightWrist, HumanoidBoneId.RightHand),
    ];

    public static void Draw(
        RayGameContext ctx,
        SkeletonFrame skeleton,
        bool isPlayer,
        bool hitFlash,
        bool strikeWindow,
        bool showCutTrail)
    {
        var torso = isPlayer ? Torso : TorsoAi;
        var wood = isPlayer ? BoneWood : BoneWoodHi;

        DrawJoint(ctx, skeleton[HumanoidBoneId.Pelvis], 0.13f, Joint);
        DrawPelvisBlock(ctx, skeleton);

        foreach (var (a, b) in BonePairs)
        {
            var radius = BoneRadius(a, b);
            var color = BoneColor(a, b, torso, wood);
            DrawBone(ctx, skeleton[a], skeleton[b], radius, color);
        }

        DrawJoint(ctx, skeleton[HumanoidBoneId.Head], 0.12f, hitFlash ? Color.FromArgb(255, 255, 160, 120) : HeadTone);
        DrawJoint(ctx, skeleton[HumanoidBoneId.LeftHand], 0.055f, HandTone);
        DrawJoint(ctx, skeleton[HumanoidBoneId.RightHand], 0.055f, HandTone);
        DrawFoot(ctx, skeleton[HumanoidBoneId.LeftAnkle], skeleton[HumanoidBoneId.LeftToe]);
        DrawFoot(ctx, skeleton[HumanoidBoneId.RightAnkle], skeleton[HumanoidBoneId.RightToe]);

        DrawPivots(ctx, skeleton);
        DrawKatana(ctx, skeleton, strikeWindow, showCutTrail);
    }

    private static void DrawPelvisBlock(RayGameContext ctx, SkeletonFrame s)
    {
        var p = s[HumanoidBoneId.Pelvis];
        var c = s[HumanoidBoneId.Chest];
        var mid = Vector3.Lerp(p, c, 0.15f);
        ctx.DrawShipBox(mid, new Vector3(0.22f, 0.14f, 0.16f), JointHi);
    }

    private static void DrawPivots(RayGameContext ctx, SkeletonFrame s)
    {
        DrawJoint(ctx, s[HumanoidBoneId.LeftKnee], 0.07f, JointHi);
        DrawJoint(ctx, s[HumanoidBoneId.RightKnee], 0.07f, JointHi);
        DrawJoint(ctx, s[HumanoidBoneId.LeftElbow], 0.06f, JointHi);
        DrawJoint(ctx, s[HumanoidBoneId.RightElbow], 0.06f, JointHi);
        DrawJoint(ctx, s[HumanoidBoneId.LeftShoulder], 0.065f, Joint);
        DrawJoint(ctx, s[HumanoidBoneId.RightShoulder], 0.065f, Joint);
        DrawJoint(ctx, s[HumanoidBoneId.Neck], 0.055f, Joint);
    }

    private static void DrawKatana(RayGameContext ctx, SkeletonFrame s, bool strikeWindow, bool showCutTrail)
    {
        var root = s.BladeRoot;
        var tip = s.BladeTip;
        ctx.DrawLaserBolt(root, tip, Blade);
        var tsuba = Vector3.Lerp(root, s[HumanoidBoneId.RightHand], 0.3f);
        ctx.DrawGlowSphere(tsuba, 0.055f, Tsuba);
        ctx.DrawGlowSphere(root, 0.042f, Sageo);
        ctx.DrawGlowSphere(tip, 0.038f, BladeEdge);

        if (strikeWindow)
            ctx.DrawGlowSphereWires(tip, 0.22f, CutTrail);

        if (showCutTrail)
        {
            var mid = Vector3.Lerp(root, tip, 0.55f);
            ctx.DrawBolt(mid, tip, CutTrail);
        }
    }

    private static void DrawBone(RayGameContext ctx, Vector3 a, Vector3 b, float radius, Color color)
    {
        var dir = b - a;
        var len = dir.Length();
        if (len < 1e-4f)
            return;

        dir /= len;
        const int segments = 4;
        for (var i = 0; i < segments; i++)
        {
            var t0 = i / (float)segments;
            var t1 = (i + 1) / (float)segments;
            var r0 = radius * (1.05f - t0 * 0.15f);
            var r1 = radius * (1.05f - t1 * 0.15f);
            var p0 = a + dir * (len * t0);
            var p1 = a + dir * (len * t1);
            ctx.DrawLaserBolt(p0, p1, color);
            ctx.DrawGlowSphere(p0, r0, color);
            if (i == segments - 1)
                ctx.DrawGlowSphere(p1, r1, color);
        }

        ctx.DrawBolt(a, b, BoneWire);
    }

    private static void DrawJoint(RayGameContext ctx, Vector3 center, float radius, Color fill)
    {
        ctx.DrawGlowSphere(center, radius, fill);
        ctx.DrawGlowSphereWires(center, radius * 1.02f, BoneWire);
    }

    private static void DrawFoot(RayGameContext ctx, Vector3 ankle, Vector3 toe)
    {
        var mid = Vector3.Lerp(ankle, toe, 0.5f);
        var dir = toe - ankle;
        var side = Vector3.Normalize(Vector3.Cross(dir, Vector3.UnitY));
        _ = side;
        ctx.DrawShipBox(mid, new Vector3(0.1f, 0.04f, 0.2f), FootTone);
    }

    private static float BoneRadius(HumanoidBoneId a, HumanoidBoneId b) =>
        (a, b) switch
        {
            (HumanoidBoneId.Pelvis, _) or (_, HumanoidBoneId.Pelvis) => 0.1f,
            (HumanoidBoneId.Chest, _) or (_, HumanoidBoneId.Chest) => 0.09f,
            (HumanoidBoneId.Neck, _) or (_, HumanoidBoneId.Head) => 0.055f,
            (HumanoidBoneId.LeftHip, _) or (_, HumanoidBoneId.RightHip) => 0.085f,
            (HumanoidBoneId.LeftKnee, _) or (_, HumanoidBoneId.RightKnee) => 0.075f,
            (HumanoidBoneId.LeftClavicle, _) or (_, HumanoidBoneId.RightClavicle) => 0.05f,
            _ => 0.065f,
        };

    private static Color BoneColor(HumanoidBoneId a, HumanoidBoneId b, Color torso, Color wood)
    {
        if (a is HumanoidBoneId.SpineLower or HumanoidBoneId.SpineMid or HumanoidBoneId.Chest
            or HumanoidBoneId.Neck
            || b is HumanoidBoneId.SpineLower or HumanoidBoneId.SpineMid or HumanoidBoneId.Chest
            or HumanoidBoneId.Neck)
            return torso;

        return wood;
    }
}
