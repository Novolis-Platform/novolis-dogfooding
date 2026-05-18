namespace RandoriFight.Game;

internal readonly struct CombatMove(
    float startup,
    float active,
    float recovery,
    float range,
    float radius,
    float damage,
    float hitStun)
{
    public float Startup { get; } = startup;
    public float Active { get; } = active;
    public float Recovery { get; } = recovery;
    public float Range { get; } = range;
    public float Radius { get; } = radius;
    public float Damage { get; } = damage;
    public float HitStun { get; } = hitStun;
    public float Total => Startup + Active + Recovery;
}

internal static class CombatMoves
{
    public static readonly CombatMove Punch = new(0.07f, 0.07f, 0.2f, 1.05f, 0.42f, 8f, 0.22f);
    public static readonly CombatMove Kick = new(0.11f, 0.09f, 0.3f, 1.45f, 0.48f, 14f, 0.32f);
}
