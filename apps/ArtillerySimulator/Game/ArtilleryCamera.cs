using System.Numerics;
using RayCamera = Novolis.Raylib.Rendering.Camera;

namespace ArtillerySimulator.Game;

internal enum CameraMode
{
  Fixed,
  Chase,
}

internal sealed class ArtilleryCamera
{
  private const float SmoothRate = 8f;
  private const float FovDegrees = 52f;

  private Vector3 _smoothedTarget;
  private Vector3 _smoothedEye;
  private bool _initialized;

  public CameraMode Mode { get; private set; } = CameraMode.Fixed;

  public void ToggleMode() =>
      Mode = Mode == CameraMode.Fixed ? CameraMode.Chase : CameraMode.Fixed;

  public void SnapToGun(Vector3 gunPivot, Vector3 barrelDir)
  {
    var desired = DesiredFixed(gunPivot, barrelDir);
    _smoothedTarget = desired.target;
    _smoothedEye = desired.eye;
    _initialized = true;
  }

  public RayCamera Build(
    float deltaSeconds,
    Vector3 gunPivot,
    Vector3 barrelDir,
    ProjectileRun shot)
  {
    var (target, eye) = Mode == CameraMode.Chase && shot.Phase != ShotPhase.Ready
        ? DesiredChase(shot)
        : DesiredFixed(gunPivot, barrelDir);

    if (!_initialized)
    {
      _smoothedTarget = target;
      _smoothedEye = eye;
      _initialized = true;
    }
    else
    {
      var t = 1f - MathF.Exp(-SmoothRate * MathF.Max(deltaSeconds, 1e-4f));
      _smoothedTarget = Vector3.Lerp(_smoothedTarget, target, t);
      _smoothedEye = Vector3.Lerp(_smoothedEye, eye, t);
    }

    return RayCamera.Perspective(_smoothedEye, _smoothedTarget, Vector3.UnitY, FovDegrees);
  }

  private static (Vector3 target, Vector3 eye) DesiredFixed(Vector3 gunPivot, Vector3 barrelDir)
  {
    var target = gunPivot + barrelDir * SimulationUnits.FixedCamLookAhead + new Vector3(0f, 12f, 0f);
    var eye = target + SimulationUnits.FixedCamEyeOffset;
    return (target, eye);
  }

  private static (Vector3 target, Vector3 eye) DesiredChase(ProjectileRun shot)
  {
    var pos = shot.CurrentPosition;
    var vel = shot.CurrentVelocity;
    var speed = vel.Length();
    var lead = speed > 1f
        ? Vector3.Normalize(vel) * MathF.Min(SimulationUnits.ChaseCamLeadMax, speed * 0.15f)
        : Vector3.Zero;
    var target = pos + lead + new Vector3(0f, 8f, 0f);

    var back = speed > 1f ? -Vector3.Normalize(vel) : new Vector3(-1f, 0.2f, 0f);
    var eye = pos + back * SimulationUnits.ChaseCamBack + new Vector3(0f, SimulationUnits.ChaseCamUp, 0f);
    return (target, eye);
  }
}
