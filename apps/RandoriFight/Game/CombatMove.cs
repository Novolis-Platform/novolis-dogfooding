namespace RandoriFight.Game;

internal readonly struct CombatMove(
    float startup,
    float active,
    float recovery,
    float range,
    float radius,
    float damage,
    float hitStun,
    float strikeHeight)
{
    public float Startup { get; } = startup;
    public float Active { get; } = active;
    public float Recovery { get; } = recovery;
    public float Range { get; } = range;
    public float Radius { get; } = radius;
    public float Damage { get; } = damage;
    public float HitStun { get; } = hitStun;
    public float StrikeHeight { get; } = strikeHeight;
    public float Total => Startup + Active + Recovery;
}

internal static class CombatMoves
{
    /// <summary>Men — lift to jōdan then cut through the head line.</summary>
    public static readonly CombatMove Men = new(0.14f, 0.09f, 0.26f, 1.35f, 0.38f, 16f, 0.28f, 1.52f);

    /// <summary>Kesa-giri — diagonal cut across the torso line.</summary>
    public static readonly CombatMove Kesa = new(0.1f, 0.1f, 0.24f, 1.25f, 0.44f, 13f, 0.24f, 1.12f);

    /// <summary>Tsuki — straight thrust along the center line.</summary>
    public static readonly CombatMove Thrust = new(0.06f, 0.07f, 0.22f, 1.55f, 0.28f, 11f, 0.2f, 1.05f);
}
