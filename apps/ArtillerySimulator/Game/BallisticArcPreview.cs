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

  public static IReadOnlyList<Vector3> Build(
    GunModel gun,
    TerrainWorld terrain,
    AtmosphereModel atmosphere,
    Vector3 muzzle)
  {
    var points = new List<Vector3>(64) { muzzle };
    var sim = gun.DragEnabled ? Drag : Vacuum;
    var state = gun.CreateProjectileState(muzzle, gun.BarrelDirection());

    for (var t = 0.0; t < MaxTimeSeconds && points.Count < MaxPoints; t += DtSeconds)
    {
      var env = atmosphere.BallisticEnvironment(gun.DragEnabled, state.Position.Y);
      var prev = state.Position;
      state = sim.Step(state, DtSeconds, env);
      var p = state.Position;

      if (terrain.TrySegmentLeavesRange(prev, p, out var boundaryHit, out _))
      {
        points.Add(terrain.ProjectOntoTerrainSurface(boundaryHit));
        break;
      }

      if (!terrain.IsInside(p.X, p.Z))
      {
        points.Add(terrain.ProjectOntoTerrainSurface(p));
        break;
      }

      if (terrain.TryHeightfieldContact(p, GunModel.MuzzleRadius))
      {
        points.Add(p);
        break;
      }

      points.Add(p);
    }

    return points;
  }
}
