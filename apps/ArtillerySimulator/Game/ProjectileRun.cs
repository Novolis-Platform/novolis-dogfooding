using System.Numerics;
using Novolis.Math.Geometry;
using Novolis.Physics.Abstractions;
using Novolis.Physics.Ballistics;
using Novolis.Physics.Collision.Simple;

namespace ArtillerySimulator.Game;

internal enum ShotPhase
{
  Ready,
  InFlight,
  Impacted,
}

internal readonly struct ImpactResult
{
  public Vector3 Position { get; init; }
  public Vector3 Velocity { get; init; }
  public double TimeSeconds { get; init; }
  public float HorizontalRangeMeters { get; init; }
  public float ImpactSpeedMps { get; init; }
}

/// <summary>Ballistic integration with per-step terrain sphere sweeps.</summary>
internal sealed class ProjectileRun
{
  private const double DtSeconds = 1.0 / 120.0;
  private const float ProjectileRadius = 0.08f;
  private const int TrailSubsample = 3;
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

  public void Advance(BvhStaticWorld terrain, GunModel gun, Vector3 originForRange)
  {
    if (Phase != ShotPhase.InFlight)
      return;

    var env = new ProjectileBallisticEnvironment(9.80665, gun.DragEnabled ? 1.225 : 0);
    var sim = gun.DragEnabled ? _drag : _vacuum;
    var displacement = _state.Velocity * (float)DtSeconds;
    var sphere = new Sphere3(_state.Position, ProjectileRadius);

    if (BallisticsQueries.SweepProjectileSphere(terrain, in sphere, displacement, out var hit))
    {
      var travel = displacement.Length();
      var t = travel > 1e-8f ? (float)(hit.Distance / travel) : 0f;
      var impactPos = _state.Position + displacement * t;
      var impactVel = _state.Velocity;
      RecordImpact(impactPos, impactVel, originForRange);
      return;
    }

    _state = sim.Step(_state, DtSeconds, env);
    _stepCounter++;
    if (_stepCounter % TrailSubsample == 0)
      _trail.Add(_state.Position);

    if (_stepCounter >= MaxSteps)
    {
      RecordImpact(_state.Position, _state.Velocity, originForRange);
    }
  }

  private void RecordImpact(Vector3 position, Vector3 velocity, Vector3 originForRange)
  {
    var horizontal = position - originForRange;
    horizontal.Y = 0f;
    Impact = new ImpactResult
    {
      Position = position,
      Velocity = velocity,
      TimeSeconds = _state.TimeSeconds,
      HorizontalRangeMeters = horizontal.Length(),
      ImpactSpeedMps = velocity.Length(),
    };
    _trail.Add(position);
    Phase = ShotPhase.Impacted;
  }
}
