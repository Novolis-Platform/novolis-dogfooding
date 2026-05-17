using System.Numerics;
using Novolis.Math.Geometry;
using Novolis.Physics.Ballistics;
using Novolis.Physics.Collision.Simple;

namespace ArtillerySimulator.Game;

internal enum ShotPhase
{
  Ready,
  InFlight,
  Impacted,
}

internal enum ImpactEndReason
{
  TerrainMesh,
  Heightfield,
  BeyondRange,
  MaxSteps,
}

internal readonly struct ImpactResult
{
  public Vector3 Position { get; init; }
  public Vector3 Velocity { get; init; }
  public double TimeSeconds { get; init; }
  public float HorizontalRangeMeters { get; init; }
  public float ImpactSpeedMps { get; init; }
  public ImpactEndReason Reason { get; init; }
}

/// <summary>Ballistic integration with integrate-then-sweep terrain contact (see physics INTEGRATION.md §3).</summary>
internal sealed class ProjectileRun
{
  public const double DtSeconds = 1.0 / 120.0;
  private const float ProjectileRadius = GunModel.MuzzleRadius;
  private const float MaxSweepMeters = 1.5f;
  private const int MaxSteps = 120_000;

  private readonly ProjectileBallisticSimulation _vacuum = new();
  private readonly ProjectileBallisticSimulation _drag = new(GunModel.Educational155Profile);
  private readonly List<Vector3> _trail = [];
  private ProjectileState _state;
  private int _stepCounter;

  public ShotPhase Phase { get; private set; } = ShotPhase.Ready;
  public IReadOnlyList<Vector3> Trail => _trail;
  public ImpactResult? Impact { get; private set; }
  public Vector3 CurrentPosition => _state.Position;
  public Vector3 CurrentVelocity => _state.Velocity;
  public double TimeSeconds => _state.TimeSeconds;

  public void Begin(ProjectileState start)
  {
    _state = start;
    _trail.Clear();
    _trail.Add(start.Position);
    _stepCounter = 0;
    Impact = null;
    Phase = ShotPhase.InFlight;
  }

  public void Reset()
  {
    _trail.Clear();
    Impact = null;
    Phase = ShotPhase.Ready;
  }

  public void AdvanceWithBudget(
    TerrainWorld terrain,
    GunModel gun,
    Vector3 originForRange,
    int maxPhysicsSteps)
  {
    for (var i = 0; i < maxPhysicsSteps && Phase == ShotPhase.InFlight; i++)
      AdvanceOne(terrain, gun, originForRange);
  }

  private void AdvanceOne(TerrainWorld terrain, GunModel gun, Vector3 originForRange)
  {
    if (Phase != ShotPhase.InFlight)
      return;

    var env = new ProjectileBallisticEnvironment(9.80665, gun.DragEnabled ? 1.225 : 0);
    var sim = gun.DragEnabled ? _drag : _vacuum;
    var collision = terrain.Collision;
    var startPos = _state.Position;
    var startVel = _state.Velocity;
    var startTime = _state.TimeSeconds;

    var candidate = sim.Step(_state, DtSeconds, env);
    var displacement = candidate.Position - startPos;
    var dist = displacement.Length();

    if (TryRangeOrGroundHit(terrain, startPos, candidate.Position, startVel, startTime, DtSeconds, originForRange))
      return;

    if (dist > MaxSweepMeters)
    {
      var splits = (int)Math.Ceiling(dist / MaxSweepMeters);
      var subDt = DtSeconds / splits;
      for (var s = 0; s < splits && Phase == ShotPhase.InFlight; s++)
      {
        var subStart = _state.Position;
        var subCandidate = sim.Step(_state, subDt, env);
        var subDisp = subCandidate.Position - subStart;

        if (TryRangeOrGroundHit(terrain, subStart, subCandidate.Position, _state.Velocity, _state.TimeSeconds, subDt, originForRange))
          return;

        if (TryTerrainHit(terrain, collision, subDisp, subDt, out var impactPos, out var impactVel, out var impactTime, out var reason))
        {
          RecordImpact(impactPos, impactVel, impactTime, originForRange, reason);
          return;
        }

        _state = subCandidate;
        _stepCounter++;
        _trail.Add(_state.Position);
      }
    }
    else
    {
      if (TryTerrainHit(terrain, collision, displacement, DtSeconds, out var impactPos, out var impactVel, out var impactTime, out var reason))
      {
        RecordImpact(impactPos, impactVel, impactTime, originForRange, reason);
        return;
      }

      _state = candidate;
      _stepCounter++;
      _trail.Add(_state.Position);
    }

    if (Phase == ShotPhase.InFlight)
      TryFallbackContact(terrain, originForRange);

    if (_stepCounter >= MaxSteps && Phase == ShotPhase.InFlight)
      RecordImpact(_state.Position, _state.Velocity, _state.TimeSeconds, originForRange, ImpactEndReason.MaxSteps);
  }

  private bool TryRangeOrGroundHit(
    TerrainWorld terrain,
    Vector3 from,
    Vector3 to,
    Vector3 velocity,
    double startTime,
    double stepDt,
    Vector3 originForRange)
  {
    if (terrain.TrySegmentLeavesRange(from, to, out var boundaryHit, out var boundaryFrac))
    {
      var t = startTime + stepDt * boundaryFrac;
      var ground = terrain.ProjectOntoTerrainSurface(boundaryHit);
      RecordImpact(ground, velocity, t, originForRange, ImpactEndReason.BeyondRange);
      return true;
    }

    if (terrain.TryHeightfieldContact(to, ProjectileRadius))
    {
      RecordImpact(to, velocity, startTime + stepDt, originForRange, ImpactEndReason.Heightfield);
      return true;
    }

    return false;
  }

  private bool TryFallbackContact(TerrainWorld terrain, Vector3 originForRange)
  {
    var p = _state.Position;
    if (!terrain.IsInside(p.X, p.Z))
    {
      var ground = terrain.ProjectOntoTerrainSurface(p);
      RecordImpact(ground, _state.Velocity, _state.TimeSeconds, originForRange, ImpactEndReason.BeyondRange);
      return true;
    }

    if (terrain.TryHeightfieldContact(p, ProjectileRadius))
    {
      RecordImpact(p, _state.Velocity, _state.TimeSeconds, originForRange, ImpactEndReason.Heightfield);
      return true;
    }

    return false;
  }

  private bool TryTerrainHit(
    TerrainWorld terrain,
    BvhStaticWorld collision,
    Vector3 displacement,
    double stepDt,
    out Vector3 impactPos,
    out Vector3 impactVel,
    out double impactTime,
    out ImpactEndReason reason)
  {
    impactPos = default;
    impactVel = default;
    impactTime = 0;
    reason = ImpactEndReason.TerrainMesh;

    var travel = displacement.Length();
    if (travel < 1e-8f)
      return false;

    var dir = displacement / travel;
    var traveled = 0f;
    var startPos = _state.Position;
    var startVel = _state.Velocity;
    var startTime = _state.TimeSeconds;

    while (traveled < travel - 1e-6f)
    {
      var chunkLen = MathF.Min(MaxSweepMeters, travel - traveled);
      var chunk = dir * chunkLen;
      var segStart = startPos + dir * traveled;
      var segEnd = segStart + chunk;

      if (terrain.TrySegmentLeavesRange(segStart, segEnd, out var boundaryHit, out var boundaryFrac))
      {
        impactPos = terrain.ProjectOntoTerrainSurface(boundaryHit);
        impactVel = startVel;
        impactTime = startTime + stepDt * (traveled + chunkLen * boundaryFrac) / travel;
        reason = ImpactEndReason.BeyondRange;
        return true;
      }

      if (terrain.TryHeightfieldContact(segEnd, ProjectileRadius))
      {
        impactPos = segEnd;
        impactVel = startVel;
        impactTime = startTime + stepDt * (traveled + chunkLen) / travel;
        reason = ImpactEndReason.Heightfield;
        return true;
      }

      var sphere = new Sphere3(segStart, ProjectileRadius);
      if (!BallisticsQueries.SweepProjectileSphere(collision, in sphere, chunk, out var hit))
      {
        traveled += chunkLen;
        continue;
      }

      var frac = chunkLen > 1e-8f ? (float)(hit.Distance / chunkLen) : 0f;
      impactPos = sphere.Center + chunk * frac;
      impactVel = startVel;
      impactTime = startTime + stepDt * (traveled + chunkLen * frac) / travel;
      reason = ImpactEndReason.TerrainMesh;
      return true;
    }

    return false;
  }

  private void RecordImpact(
    Vector3 position,
    Vector3 velocity,
    double timeSeconds,
    Vector3 originForRange,
    ImpactEndReason reason)
  {
    var horizontal = position - originForRange;
    horizontal.Y = 0f;
    Impact = new ImpactResult
    {
      Position = position,
      Velocity = velocity,
      TimeSeconds = timeSeconds,
      HorizontalRangeMeters = horizontal.Length(),
      ImpactSpeedMps = velocity.Length(),
      Reason = reason,
    };
    _trail.Add(position);
    Phase = ShotPhase.Impacted;
  }
}
