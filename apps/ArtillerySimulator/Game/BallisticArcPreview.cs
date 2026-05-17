using System.Numerics;
using Novolis.Physics.Ballistics;

namespace ArtillerySimulator.Game;

/// <summary>Integrates a ballistic arc for aim preview (no terrain BVH).</summary>
internal static class BallisticArcPreview
{
  private const double DtSeconds = 1.0 / 60.0;
  private const double MaxTimeSeconds = 90.0;
  private const int MaxPoints = 512;

  private static readonly ProjectileBallisticSimulation Vacuum = new();
  private static readonly ProjectileBallisticSimulation Drag = new(GunModel.Educational155Profile);

  public static IReadOnlyList<Vector3> Build(GunModel gun, TerrainWorld terrain, Vector3 muzzle)
  {
    var points = new List<Vector3>(64) { muzzle };
    var env = new ProjectileBallisticEnvironment(9.80665, gun.DragEnabled ? 1.225 : 0);
    var sim = gun.DragEnabled ? Drag : Vacuum;
    var state = gun.CreateProjectileState(muzzle, gun.BarrelDirection());

    for (var t = 0.0; t < MaxTimeSeconds && points.Count < MaxPoints; t += DtSeconds)
    {
      var prev = state.Position;
      state = sim.Step(state, DtSeconds, env);
      var p = state.Position;

      if (terrain.TrySegmentLeavesRange(prev, p, out _, out _))
        break;

      if (!terrain.IsInside(p.X, p.Z))
        break;

      if (terrain.TryHeightfieldContact(p, GunModel.MuzzleRadius))
        break;

      points.Add(p);
    }

    return points;
  }
}
