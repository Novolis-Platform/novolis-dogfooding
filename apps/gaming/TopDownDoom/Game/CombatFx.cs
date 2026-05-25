using System.Numerics;

namespace TopDownDoom.Game;

internal enum CombatFxKind
{
    MuzzleFlash,
    HitSpark,
    BloodSplat,
    Explosion,
}

internal sealed class CombatFx(CombatFxKind kind, Vector3 position, float duration, float scale = 1f)
{
    public CombatFxKind Kind = kind;
    public Vector3 Position = position;
    public float Duration = duration;
    public float Time;
    public float Scale = scale;
}

internal sealed class CombatJuice
{
    public readonly List<CombatFx> Effects = [];
    public float Shake;

    public void Tick(float dt)
    {
        Shake = MathF.Max(0f, Shake - dt * 2.8f);
        for (var i = Effects.Count - 1; i >= 0; i--)
        {
            Effects[i].Time += dt;
            if (Effects[i].Time >= Effects[i].Duration)
            {
                Effects.RemoveAt(i);
            }
        }
    }

    public void Add(CombatFxKind kind, Vector3 position, float duration, float scale = 1f)
    {
        Effects.Add(new CombatFx(kind, position, duration, scale));
        Shake = kind switch
        {
            CombatFxKind.Explosion => MathF.Min(1.2f, Shake + 0.55f * scale),
            CombatFxKind.MuzzleFlash => MathF.Min(0.5f, Shake + 0.08f),
            CombatFxKind.HitSpark => MathF.Min(0.7f, Shake + 0.12f),
            _ => MathF.Min(0.4f, Shake + 0.06f),
        };
    }
}
