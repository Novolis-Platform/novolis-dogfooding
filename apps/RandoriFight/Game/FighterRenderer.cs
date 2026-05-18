using System.Drawing;
using Novolis.Raylib.Game;
using RandoriFight.Game.Skeleton;

namespace RandoriFight.Game;

internal static class FighterRenderer
{
    public static void Draw(RayGameContext ctx, Fighter fighter, bool isPlayer)
    {
        var landmarks = fighter.CurrentPose;
        var skeleton = HumanoidSkeleton.SolveFromLandmarks(landmarks, fighter.WorldPosition, fighter.Facing);
        var hitFlash = fighter.State == FighterState.HitStun;
        var cutTrail = fighter.IsAttacking && fighter.MoveNormalizedTime is > 0.32f and < 0.68f;

        PosableMannequinRenderer.Draw(
            ctx,
            skeleton,
            isPlayer,
            hitFlash,
            fighter.IsStrikeWindow,
            cutTrail);
    }
}
