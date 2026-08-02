using System.Numerics;

namespace KatoriLab.Demo;

/// <summary>Named grip / tip points in bokken local space (+Z along blade, centered at origin).</summary>
internal readonly record struct KenHoldPoint(string Name, Vector3 LocalPosition);

/// <summary>Hold-point set for a bokken / ken (two-hand tsuka + kashira + kissaki).</summary>
internal sealed class KenHoldSet
{
    public KenHoldSet(
        KenHoldPoint primaryGrip,
        KenHoldPoint secondaryGrip,
        KenHoldPoint kashira,
        KenHoldPoint kissaki)
    {
        PrimaryGrip = primaryGrip;
        SecondaryGrip = secondaryGrip;
        Kashira = kashira;
        Kissaki = kissaki;
    }

    /// <summary>Right hand (shimo / toward kashira).</summary>
    public KenHoldPoint PrimaryGrip { get; }

    /// <summary>Left hand (kami / toward tsuba).</summary>
    public KenHoldPoint SecondaryGrip { get; }

    public KenHoldPoint Kashira { get; }
    public KenHoldPoint Kissaki { get; }

    public IEnumerable<KenHoldPoint> All
    {
        get
        {
            yield return PrimaryGrip;
            yield return SecondaryGrip;
            yield return Kashira;
            yield return Kissaki;
        }
    }

    /// <summary>Builds holds for a bokken whose longest axis is +Z and centered at origin.</summary>
    public static KenHoldSet ForCenteredBokken(float lengthMeters)
    {
        var half = lengthMeters * 0.5f;
        return new KenHoldSet(
            new KenHoldPoint("primary", new Vector3(0.02f, -0.02f, -half * 0.55f)),
            new KenHoldPoint("secondary", new Vector3(-0.01f, 0.01f, -half * 0.22f)),
            new KenHoldPoint("kashira", new Vector3(0f, 0f, -half * 0.95f)),
            new KenHoldPoint("kissaki", new Vector3(0f, 0f, half * 0.95f)));
    }

    public Vector3 World(KenHoldPoint point, Matrix4x4 weaponWorld) =>
        Vector3.Transform(point.LocalPosition, weaponWorld);
}
