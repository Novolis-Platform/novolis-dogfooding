namespace RandoriFight.Game;

internal enum FighterState
{
    /// <summary>Chūdan-no-kamae — middle guard, blade toward opponent.</summary>
    Idle,
    Walk,
    /// <summary>Men — vertical cut to the head line.</summary>
    Men,
    /// <summary>Kesa — diagonal cut.</summary>
    Kesa,
    /// <summary>Tsuki — thrust.</summary>
    Thrust,
    /// <summary>Uke — receiving with blade in line.</summary>
    Parry,
    HitStun,
    Ko,
}
