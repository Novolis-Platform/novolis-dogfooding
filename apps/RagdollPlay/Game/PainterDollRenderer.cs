using System.Drawing;
using System.Numerics;
using Novolis.Physics.Collision.Simple;
using Novolis.Raylib.Game;

namespace RagdollPlay.Game;

/// <summary>Wooden artist-mannequin visuals over sphere-chain physics.</summary>
internal static class PainterDollRenderer
{
    private static readonly Color Wood = Color.FromArgb(255, 196, 158, 108);
    private static readonly Color WoodDark = Color.FromArgb(255, 142, 98, 58);
    private static readonly Color WoodWire = Color.FromArgb(255, 88, 58, 32);
    private static readonly Color JointKnob = Color.FromArgb(255, 118, 78, 42);
    private static readonly Color HeadTone = Color.FromArgb(255, 220, 188, 140);

    public static void Draw(RayGameContext ctx, IReadOnlyList<SphereState> bones)
    {
        if (bones.Count < 10)
            return;

        Vector3 P(int i) => bones[i].Position;

        DrawPelvis(ctx, P(RagdollIndices.Pelvis), P(RagdollIndices.LeftKnee), P(RagdollIndices.RightKnee), P(RagdollIndices.Hip));
        DrawBone(ctx, P(RagdollIndices.LeftKnee), P(RagdollIndices.Hip), 0.11f);
        DrawBone(ctx, P(RagdollIndices.RightKnee), P(RagdollIndices.Hip), 0.11f);
        DrawBone(ctx, P(RagdollIndices.Pelvis), P(RagdollIndices.LeftKnee), 0.09f);
        DrawBone(ctx, P(RagdollIndices.Pelvis), P(RagdollIndices.RightKnee), 0.09f);
        DrawBone(ctx, P(RagdollIndices.Hip), P(RagdollIndices.Chest), 0.2f, 0.16f);
        DrawBone(ctx, P(RagdollIndices.Chest), P(RagdollIndices.LeftShoulder), 0.08f);
        DrawBone(ctx, P(RagdollIndices.Chest), P(RagdollIndices.RightShoulder), 0.08f);
        DrawBone(ctx, P(RagdollIndices.LeftShoulder), P(RagdollIndices.LeftHand), 0.07f);
        DrawBone(ctx, P(RagdollIndices.RightShoulder), P(RagdollIndices.RightHand), 0.07f);

        DrawNeck(ctx, P(RagdollIndices.Chest), P(RagdollIndices.Head));
        DrawHead(ctx, P(RagdollIndices.Head));

        DrawJoint(ctx, P(RagdollIndices.Pelvis), 0.1f);
        DrawJoint(ctx, P(RagdollIndices.LeftKnee), 0.085f);
        DrawJoint(ctx, P(RagdollIndices.RightKnee), 0.085f);
        DrawJoint(ctx, P(RagdollIndices.Hip), 0.11f);
        DrawJoint(ctx, P(RagdollIndices.Chest), 0.1f);
        DrawJoint(ctx, P(RagdollIndices.LeftShoulder), 0.075f);
        DrawJoint(ctx, P(RagdollIndices.RightShoulder), 0.075f);
        DrawJoint(ctx, P(RagdollIndices.LeftHand), 0.065f);
        DrawJoint(ctx, P(RagdollIndices.RightHand), 0.065f);
    }

    public static bool TryPickBone(
        Vector3 rayOrigin,
        Vector3 rayDir,
        IReadOnlyList<SphereState> bones,
        float pickRadius,
        out int sphereIndex,
        out float hitDistance)
    {
        sphereIndex = -1;
        hitDistance = float.MaxValue;

        if (bones.Count < 10)
            return false;

        Vector3 P(int i) => bones[i].Position;

        TrySegment(rayOrigin, rayDir, P(RagdollIndices.Head), 0.22f, RagdollIndices.Head, pickRadius, ref sphereIndex, ref hitDistance);
        TrySegment(rayOrigin, rayDir, P(RagdollIndices.Chest), P(RagdollIndices.Head), RagdollIndices.Chest, pickRadius, ref sphereIndex, ref hitDistance);
        TrySegment(rayOrigin, rayDir, P(RagdollIndices.Hip), P(RagdollIndices.Chest), RagdollIndices.Hip, pickRadius, ref sphereIndex, ref hitDistance);
        TrySegment(rayOrigin, rayDir, P(RagdollIndices.LeftKnee), P(RagdollIndices.Hip), RagdollIndices.LeftKnee, pickRadius, ref sphereIndex, ref hitDistance);
        TrySegment(rayOrigin, rayDir, P(RagdollIndices.RightKnee), P(RagdollIndices.Hip), RagdollIndices.RightKnee, pickRadius, ref sphereIndex, ref hitDistance);
        TrySegment(rayOrigin, rayDir, P(RagdollIndices.Pelvis), P(RagdollIndices.LeftKnee), RagdollIndices.Pelvis, pickRadius, ref sphereIndex, ref hitDistance);
        TrySegment(rayOrigin, rayDir, P(RagdollIndices.Pelvis), P(RagdollIndices.RightKnee), RagdollIndices.Pelvis, pickRadius, ref sphereIndex, ref hitDistance);
        TrySegment(rayOrigin, rayDir, P(RagdollIndices.Chest), P(RagdollIndices.LeftShoulder), RagdollIndices.LeftShoulder, pickRadius, ref sphereIndex, ref hitDistance);
        TrySegment(rayOrigin, rayDir, P(RagdollIndices.Chest), P(RagdollIndices.RightShoulder), RagdollIndices.RightShoulder, pickRadius, ref sphereIndex, ref hitDistance);
        TrySegment(rayOrigin, rayDir, P(RagdollIndices.LeftShoulder), P(RagdollIndices.LeftHand), RagdollIndices.LeftHand, pickRadius, ref sphereIndex, ref hitDistance);
        TrySegment(rayOrigin, rayDir, P(RagdollIndices.RightShoulder), P(RagdollIndices.RightHand), RagdollIndices.RightHand, pickRadius, ref sphereIndex, ref hitDistance);

        for (var i = 0; i < bones.Count; i++)
        {
            if (RaySphere.TryHit(rayOrigin, rayDir, bones[i].Position, pickRadius, out var t) && t >= 0f && t < hitDistance)
            {
                hitDistance = t;
                sphereIndex = i;
            }
        }

        return sphereIndex >= 0;
    }

    private static void DrawPelvis(RayGameContext ctx, Vector3 pelvis, Vector3 leftKnee, Vector3 rightKnee, Vector3 hip)
    {
        var center = (pelvis + hip) * 0.5f;
        var width = Vector3.Distance(leftKnee, rightKnee) + 0.18f;
        ctx.DrawShipBox(center, new Vector3(width, 0.14f, 0.12f), WoodDark);
        ctx.DrawShipWires(center, new Vector3(width, 0.14f, 0.12f), WoodWire);
    }

    private static void DrawNeck(RayGameContext ctx, Vector3 chest, Vector3 head)
    {
        var top = head + new Vector3(0f, -0.12f, 0f);
        DrawBone(ctx, chest, top, 0.07f);
    }

    private static void DrawHead(RayGameContext ctx, Vector3 center)
    {
        ctx.DrawGlowSphere(center + new Vector3(0f, 0.04f, 0f), 0.2f, HeadTone);
        ctx.DrawGlowSphereWires(center + new Vector3(0f, 0.04f, 0f), 0.2f, WoodWire);
    }

    private static void DrawJoint(RayGameContext ctx, Vector3 center, float radius)
    {
        ctx.DrawGlowSphere(center, radius, JointKnob);
        ctx.DrawGlowSphereWires(center, radius, WoodWire);
    }

    private static void DrawBone(RayGameContext ctx, Vector3 a, Vector3 b, float thickness, float? depth = null)
    {
        var delta = b - a;
        var len = delta.Length();
        if (len < 1e-4f)
            return;

        var mid = (a + b) * 0.5f;
        var dir = delta / len;
        var t = thickness;
        var d = depth ?? thickness;
        var size = DominantAxisSize(dir, len, t, d);
        ctx.DrawShipBox(mid, size, Wood);
        ctx.DrawShipWires(mid, size, WoodWire);
    }

    private static Vector3 DominantAxisSize(Vector3 dir, float length, float thickness, float depth)
    {
        var ax = MathF.Abs(dir.X);
        var ay = MathF.Abs(dir.Y);
        var az = MathF.Abs(dir.Z);
        if (ay >= ax && ay >= az)
            return new Vector3(thickness, length, depth);
        if (ax >= az)
            return new Vector3(length, thickness, depth);
        return new Vector3(thickness, depth, length);
    }

    private static void TrySegment(
        Vector3 origin,
        Vector3 dir,
        Vector3 a,
        Vector3 b,
        int index,
        float radius,
        ref int bestIndex,
        ref float bestT)
    {
        if (!RayCapsule.TryHit(origin, dir, a, b, radius, out var t))
            return;
        if (t < bestT)
        {
            bestT = t;
            bestIndex = index;
        }
    }

    private static void TrySegment(
        Vector3 origin,
        Vector3 dir,
        Vector3 center,
        float radius,
        int index,
        float pickRadius,
        ref int bestIndex,
        ref float bestT)
    {
        if (!RaySphere.TryHit(origin, dir, center, radius + pickRadius, out var t))
            return;
        if (t >= 0f && t < bestT)
        {
            bestT = t;
            bestIndex = index;
        }
    }
}

internal static class RayCapsule
{
    public static bool TryHit(Vector3 origin, Vector3 dir, Vector3 a, Vector3 b, float radius, out float t)
    {
        var ab = b - a;
        var abLenSq = ab.LengthSquared();
        if (abLenSq < 1e-8f)
            return RaySphere.TryHit(origin, dir, a, radius, out t);

        var ao = origin - a;
        var dab = Vector3.Dot(dir, ab);
        var daa = Vector3.Dot(dir, ao);
        var aab = Vector3.Dot(ab, ao);

        var aCoeff = Vector3.Dot(dir, dir) - dab * dab / abLenSq;
        var bCoeff = 2f * (daa - dab * aab / abLenSq);
        var cCoeff = Vector3.Dot(ao, ao) - aab * aab / abLenSq - radius * radius;

        if (MathF.Abs(aCoeff) < 1e-8f)
        {
            t = -1f;
            return false;
        }

        var disc = bCoeff * bCoeff - 4f * aCoeff * cCoeff;
        if (disc < 0f)
        {
            t = -1f;
            return false;
        }

        t = (-bCoeff - MathF.Sqrt(disc)) / (2f * aCoeff);
        if (t < 0f)
        {
            t = -1f;
            return false;
        }

        var u = Math.Clamp((aab + t * dab) / abLenSq, 0f, 1f);
        var closest = a + ab * u;
        var distSq = Vector3.Cross(dir, origin - closest).LengthSquared();
        if (distSq > radius * radius * 4f)
        {
            t = -1f;
            return false;
        }

        return true;
    }
}
