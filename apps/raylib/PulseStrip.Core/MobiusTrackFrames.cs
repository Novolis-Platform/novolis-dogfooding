namespace PulseStrip.Core;

using System.Numerics;

/// <summary>
/// Frenet-style road frames with Möbius twist + extra swirl along a 3D centerline.
/// </summary>
public static class MobiusTrackFrames
{
    /// <summary>Classic Möbius: one half-twist (π) per lap.</summary>
    public const float HalfTwistsPerLap = 1f;

    /// <summary>Additional full rolls per lap on top of the Möbius half-twist.</summary>
    public const float ExtraSwirlsPerLap = 2f;

    public readonly record struct SurfaceFrame(
        Vector3 Position,
        Vector3 Tangent,
        Vector3 Right,
        Vector3 Up,
        float TwistRadians,
        float LoopT);

    public static SurfaceFrame[] Build(IReadOnlyList<Vector3> centerline)
    {
        var n = centerline.Count;
        if (n < 3)
            return [];

        var frames = new SurfaceFrame[n];
        for (var i = 0; i < n; i++)
        {
            var prev = centerline[(i - 1 + n) % n];
            var cur = centerline[i];
            var next = centerline[(i + 1) % n];
            var tangent = next - prev;
            if (tangent.LengthSquared() < 1e-8f)
                tangent = next - cur;
            if (tangent.LengthSquared() < 1e-8f)
                tangent = Vector3.UnitX;
            tangent = Vector3.Normalize(tangent);

            var loopT = i / (float)n;
            var twist = MathF.PI * HalfTwistsPerLap * loopT
                        + MathF.Tau * ExtraSwirlsPerLap * loopT;

            // Reference frame: keep a stable up preference, then spin around tangent.
            var refRight = Vector3.Cross(Vector3.UnitY, tangent);
            if (refRight.LengthSquared() < 1e-6f)
                refRight = Vector3.Cross(Vector3.UnitX, tangent);
            refRight = Vector3.Normalize(refRight);
            var refUp = Vector3.Normalize(Vector3.Cross(tangent, refRight));

            var cos = MathF.Cos(twist);
            var sin = MathF.Sin(twist);
            var right = refRight * cos + refUp * sin;
            var up = -refRight * sin + refUp * cos;

            frames[i] = new SurfaceFrame(cur, tangent, right, up, twist, loopT);
        }

        return frames;
    }

    public static SurfaceFrame Nearest(IReadOnlyList<SurfaceFrame> frames, Vector3 world)
    {
        if (frames.Count == 0)
            return default;

        var best = 0;
        var bestD = float.MaxValue;
        for (var i = 0; i < frames.Count; i++)
        {
            var d = Vector3.DistanceSquared(frames[i].Position, world);
            if (d < bestD)
            {
                bestD = d;
                best = i;
            }
        }

        return frames[best];
    }
}
