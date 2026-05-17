using System.Numerics;

namespace DoomLite3D.Game;

internal static class CombatRaycast
{
    /// <summary>
    /// Tests whether a look ray hits an enemy column on the XZ plane (ignores pitch/Y).
    /// </summary>
    public static bool TryHitEnemyXZ(
        Vector3 rayOrigin,
        Vector3 rayDirection,
        Vector3 enemyPosition,
        float maxRange,
        float hitRadius,
        out float distanceAlongRay)
    {
        distanceAlongRay = 0f;

        var ox = rayOrigin.X;
        var oz = rayOrigin.Z;
        var dx = rayDirection.X;
        var dz = rayDirection.Z;
        var lenSq = dx * dx + dz * dz;
        if (lenSq < 1e-8f)
            return false;

        var invLen = 1f / MathF.Sqrt(lenSq);
        dx *= invLen;
        dz *= invLen;

        var ex = enemyPosition.X;
        var ez = enemyPosition.Z;
        var t = (ex - ox) * dx + (ez - oz) * dz;
        if (t < 0.25f || t > maxRange)
            return false;

        var closestX = ox + dx * t;
        var closestZ = oz + dz * t;
        var missX = closestX - ex;
        var missZ = closestZ - ez;
        var missSq = missX * missX + missZ * missZ;
        if (missSq > hitRadius * hitRadius)
            return false;

        distanceAlongRay = t;
        return true;
    }
}
