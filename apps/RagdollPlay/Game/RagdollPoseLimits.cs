using System.Numerics;
using Novolis.Physics.Collision.Simple;
using Novolis.Physics.Joints;

namespace RagdollPlay.Game;

internal static class RagdollPoseLimits
{
    private const float Deg = MathF.PI / 180f;

    public static void BuildFromSpawnPose(
        IReadOnlyList<SphereState> spheres,
        List<SwingLimit> swingLimits,
        List<HingeLimit> hingeLimits)
    {
        swingLimits.Clear();
        hingeLimits.Clear();

        AddSwing(spheres, swingLimits, RagdollIndices.Hip, RagdollIndices.Chest, maxDegrees: 36f, stiffness: 1f);
        AddSwing(spheres, swingLimits, RagdollIndices.Chest, RagdollIndices.Head, maxDegrees: 50f, stiffness: 0.95f);
        AddSwing(spheres, swingLimits, RagdollIndices.Chest, RagdollIndices.LeftShoulder, maxDegrees: 76f, stiffness: 0.9f);
        AddSwing(spheres, swingLimits, RagdollIndices.Chest, RagdollIndices.RightShoulder, maxDegrees: 76f, stiffness: 0.9f);

        AddKnee(spheres, hingeLimits, RagdollIndices.Hip, RagdollIndices.LeftKnee, hingeAxis: Vector3.UnitX);
        AddKnee(spheres, hingeLimits, RagdollIndices.Hip, RagdollIndices.RightKnee, hingeAxis: -Vector3.UnitX);

        AddElbow(spheres, hingeLimits, RagdollIndices.LeftShoulder, RagdollIndices.LeftHand, lateralSign: 1f);
        AddElbow(spheres, hingeLimits, RagdollIndices.RightShoulder, RagdollIndices.RightHand, lateralSign: -1f);
    }

    private static void AddSwing(
        IReadOnlyList<SphereState> spheres,
        List<SwingLimit> limits,
        int parent,
        int child,
        float maxDegrees,
        float stiffness)
    {
        var rest = BoneDirection(spheres, parent, child);
        limits.Add(new SwingLimit(parent, child, rest, maxDegrees * Deg, stiffness));
    }

    private static void AddKnee(
        IReadOnlyList<SphereState> spheres,
        List<HingeLimit> limits,
        int parent,
        int child,
        Vector3 hingeAxis)
    {
        var rest = BoneDirection(spheres, parent, child);
        limits.Add(new HingeLimit(
            parent,
            child,
            hingeAxis,
            rest,
            minRadians: -8f * Deg,
            maxRadians: 112f * Deg,
            stiffness: 0.98f));
    }

    private static void AddElbow(
        IReadOnlyList<SphereState> spheres,
        List<HingeLimit> limits,
        int parent,
        int child,
        float lateralSign)
    {
        var rest = BoneDirection(spheres, parent, child);
        var forward = Vector3.UnitZ;
        var axis = Vector3.Normalize(Vector3.Cross(rest, forward) + new Vector3(0.02f * lateralSign, 0f, 0f));
        if (float.IsNaN(axis.X))
            axis = new Vector3(lateralSign, 0f, 0f);

        limits.Add(new HingeLimit(
            parent,
            child,
            axis,
            rest,
            minRadians: -6f * Deg,
            maxRadians: 132f * Deg,
            stiffness: 0.96f));
    }

    private static Vector3 BoneDirection(IReadOnlyList<SphereState> spheres, int parent, int child) =>
        Vector3.Normalize(spheres[child].Position - spheres[parent].Position);
}
