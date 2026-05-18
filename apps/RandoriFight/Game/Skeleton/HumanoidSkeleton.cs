using System.Numerics;
using RandoriFight.Game;

namespace RandoriFight.Game.Skeleton;

/// <summary>Builds a full posable skeleton from katana pose landmarks (two-bone IK limbs + spine chain).</summary>
internal static class HumanoidSkeleton
{
    private const float UpperLeg = 0.44f;
    private const float LowerLeg = 0.44f;
    private const float UpperArm = 0.3f;
    private const float LowerArm = 0.28f;

    public static SkeletonFrame SolveFromLandmarks(KatanaPose pose, Vector3 worldRoot, int facing)
    {
        var frame = new SkeletonFrame();
        var pelvis = World(worldRoot, facing, pose.Hips);
        var chest = World(worldRoot, facing, pose.Chest);
        var head = World(worldRoot, facing, pose.Head);
        var lFoot = World(worldRoot, facing, pose.LeftFoot);
        var rFoot = World(worldRoot, facing, pose.RightFoot);
        var lHand = World(worldRoot, facing, pose.LeftHand);
        var rHand = World(worldRoot, facing, pose.RightHand);
        var bladeRoot = World(worldRoot, facing, pose.BladeRoot);
        var bladeTip = World(worldRoot, facing, pose.BladeTip);

        frame.Set(HumanoidBoneId.Pelvis, pelvis);
        frame.Set(HumanoidBoneId.SpineLower, Vector3.Lerp(pelvis, chest, 0.33f));
        frame.Set(HumanoidBoneId.SpineMid, Vector3.Lerp(pelvis, chest, 0.66f));
        frame.Set(HumanoidBoneId.Chest, chest);
        frame.Set(HumanoidBoneId.Neck, Vector3.Lerp(chest, head, 0.38f));
        frame.Set(HumanoidBoneId.Head, head);

        SolveLeg(frame, HumanoidBoneId.LeftHip, HumanoidBoneId.LeftKnee, HumanoidBoneId.LeftAnkle, HumanoidBoneId.LeftToe,
            pelvis, lFoot, hipSocket: new(-0.13f, 0.02f, 0.06f), facing, bendSign: 1f);
        SolveLeg(frame, HumanoidBoneId.RightHip, HumanoidBoneId.RightKnee, HumanoidBoneId.RightAnkle, HumanoidBoneId.RightToe,
            pelvis, rFoot, hipSocket: new(0.1f, 0.01f, -0.05f), facing, bendSign: -1f);

        var lClav = chest + Local(facing, new(-0.15f, 0.12f, 0.02f));
        var rClav = chest + Local(facing, new(0.15f, 0.12f, 0.02f));
        frame.Set(HumanoidBoneId.LeftClavicle, lClav);
        frame.Set(HumanoidBoneId.RightClavicle, rClav);

        SolveArm(frame, HumanoidBoneId.LeftShoulder, HumanoidBoneId.LeftElbow, HumanoidBoneId.LeftWrist, HumanoidBoneId.LeftHand,
            lClav, lHand, facing, bendSign: -1f);
        SolveArm(frame, HumanoidBoneId.RightShoulder, HumanoidBoneId.RightElbow, HumanoidBoneId.RightWrist, HumanoidBoneId.RightHand,
            rClav, rHand, facing, bendSign: 1f);

        frame.Set(HumanoidBoneId.BladeRoot, bladeRoot);
        frame.Set(HumanoidBoneId.BladeTip, bladeTip);

        return frame;
    }

    private static void SolveLeg(
        SkeletonFrame frame,
        HumanoidBoneId hipId,
        HumanoidBoneId kneeId,
        HumanoidBoneId ankleId,
        HumanoidBoneId toeId,
        Vector3 pelvis,
        Vector3 foot,
        Vector3 hipSocket,
        int facing,
        float bendSign)
    {
        var hip = pelvis + Local(facing, hipSocket);
        var knee = TwoBoneJoint(hip, foot, UpperLeg, LowerLeg, new Vector3(0f, 0f, bendSign * 0.85f));
        var ankle = foot + new Vector3(0f, 0.06f, 0f);
        var toe = foot + Local(facing, new(0.06f, 0f, 0.1f));

        frame.Set(hipId, hip);
        frame.Set(kneeId, knee);
        frame.Set(ankleId, ankle);
        frame.Set(toeId, toe);
    }

    private static void SolveArm(
        SkeletonFrame frame,
        HumanoidBoneId shoulderId,
        HumanoidBoneId elbowId,
        HumanoidBoneId wristId,
        HumanoidBoneId handId,
        Vector3 shoulderRoot,
        Vector3 handTarget,
        int facing,
        float bendSign)
    {
        var shoulder = shoulderRoot + Local(facing, new(0f, -0.02f, 0f));
        var elbow = TwoBoneJoint(shoulder, handTarget, UpperArm, LowerArm, new Vector3(0f, 0f, bendSign * 0.7f));
        var wrist = Vector3.Lerp(elbow, handTarget, 0.58f);

        frame.Set(shoulderId, shoulder);
        frame.Set(elbowId, elbow);
        frame.Set(wristId, wrist);
        frame.Set(handId, handTarget);
    }

    private static Vector3 TwoBoneJoint(Vector3 root, Vector3 target, float upperLen, float lowerLen, Vector3 bendHint)
    {
        var toTarget = target - root;
        var dist = toTarget.Length();
        if (dist < 1e-5f)
            return root + new Vector3(0f, -upperLen, 0f);

        var dir = toTarget / dist;
        var maxReach = upperLen + lowerLen - 0.01f;
        dist = Math.Min(dist, maxReach);

        var cosAngle = (upperLen * upperLen + dist * dist - lowerLen * lowerLen) / (2f * upperLen * dist);
        cosAngle = Math.Clamp(cosAngle, -1f, 1f);
        var angle = MathF.Acos(cosAngle);

        var axis = Vector3.Cross(dir, bendHint);
        if (axis.LengthSquared() < 1e-6f)
            axis = Vector3.Cross(dir, Vector3.UnitY);
        axis = Vector3.Normalize(axis);

        var upperDir = Rotate(dir, axis, MathF.PI - angle);
        return root + upperDir * upperLen;
    }

    private static Vector3 Rotate(Vector3 v, Vector3 axis, float angle)
    {
        var c = MathF.Cos(angle);
        var s = MathF.Sin(angle);
        return v * c + Vector3.Cross(axis, v) * s + axis * Vector3.Dot(axis, v) * (1f - c);
    }

    private static Vector3 World(Vector3 root, int facing, Vector3 local) =>
        root + Local(facing, local);

    private static Vector3 Local(int facing, Vector3 local) =>
        new(local.X * facing, local.Y, local.Z);
}
