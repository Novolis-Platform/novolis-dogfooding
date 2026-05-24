using System.Numerics;

namespace XFighter.Game;

public static class CombatSystem
{
    public static bool SegmentHitsSphere(Vector3 segStart, Vector3 segEnd, Vector3 center, float radius)
    {
        var ab = segEnd - segStart;
        var ac = center - segStart;
        var abLenSq = ab.X * ab.X + ab.Y * ab.Y + ab.Z * ab.Z;
        if (abLenSq < 1e-8f)
            return Vector3.Distance(segStart, center) <= radius;

        var t = Math.Clamp((ac.X * ab.X + ac.Y * ab.Y + ac.Z * ab.Z) / abLenSq, 0f, 1f);
        var closest = segStart + ab * t;
        return Vector3.Distance(closest, center) <= radius;
    }
}

internal sealed class LaserBolt
{
    public Vector3 Position;
    public Vector3 Velocity;
    public float Life;
    public bool Active;
}

internal sealed class Explosion
{
    public Vector3 Position;
    public float Life;
    public float MaxLife;
    public bool Active;
}
