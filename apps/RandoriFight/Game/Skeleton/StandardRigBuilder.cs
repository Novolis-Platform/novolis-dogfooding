using System.Drawing;
using System.Numerics;

namespace RandoriFight.Game.Skeleton;

/// <summary>Maps solved joints to a standard T-rig (Hips, Spine, Head, Arm, ForeArm, Hand, UpLeg, Leg, Foot).</summary>
internal static class StandardRigBuilder
{
    public static IReadOnlyList<RigSegment> BuildSegments(SkeletonFrame s, bool isPlayer)
    {
        var torso = isPlayer
            ? Color.FromArgb(255, 210, 198, 178)
            : Color.FromArgb(255, 188, 198, 218);
        var limb = isPlayer
            ? Color.FromArgb(255, 198, 158, 112)
            : Color.FromArgb(255, 178, 188, 208);
        var head = Color.FromArgb(255, 215, 175, 135);
        var dark = Color.FromArgb(255, 42, 40, 48);

        return
        [
            Seg(s[HumanoidBoneId.Pelvis], s[HumanoidBoneId.Chest], 0.13f, torso, RigSegmentKind.Torso),
            Seg(s[HumanoidBoneId.Chest], s[HumanoidBoneId.Neck], 0.08f, torso, RigSegmentKind.Torso),
            Seg(s[HumanoidBoneId.Neck], s[HumanoidBoneId.Head], 0.1f, head, RigSegmentKind.Head),
            Seg(s[HumanoidBoneId.LeftHip], s[HumanoidBoneId.LeftKnee], 0.095f, limb, RigSegmentKind.Limb),
            Seg(s[HumanoidBoneId.LeftKnee], s[HumanoidBoneId.LeftAnkle], 0.08f, limb, RigSegmentKind.Limb),
            Seg(s[HumanoidBoneId.LeftAnkle], s[HumanoidBoneId.LeftToe], 0.055f, dark, RigSegmentKind.Extremity),
            Seg(s[HumanoidBoneId.RightHip], s[HumanoidBoneId.RightKnee], 0.095f, limb, RigSegmentKind.Limb),
            Seg(s[HumanoidBoneId.RightKnee], s[HumanoidBoneId.RightAnkle], 0.08f, limb, RigSegmentKind.Limb),
            Seg(s[HumanoidBoneId.RightAnkle], s[HumanoidBoneId.RightToe], 0.055f, dark, RigSegmentKind.Extremity),
            Seg(s[HumanoidBoneId.LeftShoulder], s[HumanoidBoneId.LeftElbow], 0.072f, limb, RigSegmentKind.Limb),
            Seg(s[HumanoidBoneId.LeftElbow], s[HumanoidBoneId.LeftHand], 0.062f, limb, RigSegmentKind.Limb),
            Seg(s[HumanoidBoneId.RightShoulder], s[HumanoidBoneId.RightElbow], 0.072f, limb, RigSegmentKind.Limb),
            Seg(s[HumanoidBoneId.RightElbow], s[HumanoidBoneId.RightHand], 0.062f, limb, RigSegmentKind.Limb),
        ];
    }

    public static IReadOnlyList<RigSegment> BuildSkeletonBones(SkeletonFrame s) =>
    [
        Seg(s[HumanoidBoneId.Pelvis], s[HumanoidBoneId.Chest], 0.05f, default, RigSegmentKind.Torso),
        Seg(s[HumanoidBoneId.Chest], s[HumanoidBoneId.Head], 0.04f, default, RigSegmentKind.Torso),
        Seg(s[HumanoidBoneId.LeftHip], s[HumanoidBoneId.LeftKnee], 0.042f, default, RigSegmentKind.Limb),
        Seg(s[HumanoidBoneId.LeftKnee], s[HumanoidBoneId.LeftAnkle], 0.038f, default, RigSegmentKind.Limb),
        Seg(s[HumanoidBoneId.RightHip], s[HumanoidBoneId.RightKnee], 0.042f, default, RigSegmentKind.Limb),
        Seg(s[HumanoidBoneId.RightKnee], s[HumanoidBoneId.RightAnkle], 0.038f, default, RigSegmentKind.Limb),
        Seg(s[HumanoidBoneId.LeftShoulder], s[HumanoidBoneId.LeftElbow], 0.032f, default, RigSegmentKind.Limb),
        Seg(s[HumanoidBoneId.LeftElbow], s[HumanoidBoneId.LeftHand], 0.028f, default, RigSegmentKind.Limb),
        Seg(s[HumanoidBoneId.RightShoulder], s[HumanoidBoneId.RightElbow], 0.032f, default, RigSegmentKind.Limb),
        Seg(s[HumanoidBoneId.RightElbow], s[HumanoidBoneId.RightHand], 0.028f, default, RigSegmentKind.Limb),
    ];

    private static RigSegment Seg(Vector3 a, Vector3 b, float radius, Color color, RigSegmentKind kind) =>
        new(a, b, radius, color, kind);
}
