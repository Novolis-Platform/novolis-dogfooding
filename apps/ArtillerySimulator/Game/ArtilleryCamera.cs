using System.Numerics;
using Novolis.Raylib.Game;
using Novolis.Raylib.Interact;
using RayCamera = Novolis.Raylib.Rendering.Camera;

namespace ArtillerySimulator.Game;

internal enum CameraMode
{
  Freecam,
  Orbit,
}

internal sealed class ArtilleryCamera
{
  private const float FovDegrees = 52f;
  private const float MouseSensitivity = 0.0022f;
  private const float FreeMoveSpeed = 1800f;
  private const float PitchLimit = MathF.PI * 0.49f;
  private const float OrbitSmoothRate = 10f;
  private const float MinOrbitDistance = 80f;
  private const float MaxOrbitDistance = 24_000f;

  private Vector3 _freePosition;
  private float _freeYaw;
  private float _freePitch;

  private Vector3 _orbitTarget;
  private float _orbitDistance = 600f;
  private float _orbitYaw;
  private float _orbitPitch = 0.35f;

  private Vector3 _smoothedOrbitTarget;
  private bool _orbitInitialized;

  public CameraMode Mode { get; private set; } = CameraMode.Freecam;

  public void ToggleMode()
  {
    Mode = Mode == CameraMode.Freecam ? CameraMode.Orbit : CameraMode.Freecam;
    _orbitInitialized = false;
  }

  public void SnapToGun(Vector3 gunPivot, Vector3 barrelDir)
  {
    _orbitTarget = gunPivot + new Vector3(0f, 4f, 0f);
    _smoothedOrbitTarget = _orbitTarget;
    _orbitYaw = MathF.Atan2(barrelDir.X, barrelDir.Z);
    _orbitPitch = 0.3f;
    _orbitDistance = 500f;
    _orbitInitialized = true;

    _freePosition = gunPivot + SimulationUnits.FixedCamEyeOffset;
    _freeYaw = _orbitYaw + MathF.PI * 0.15f;
    _freePitch = -0.12f;
  }

  public void Update(RayGameContext ctx, Vector3 gunPivot, ProjectileRun shot)
  {
    if (Mode == CameraMode.Freecam)
      UpdateFreecam(ctx);
    else
      UpdateOrbit(ctx, gunPivot, shot);
  }

  public RayCamera Build(float deltaSeconds, Vector3 gunPivot, Vector3 barrelDir, ProjectileRun shot)
  {
    if (Mode == CameraMode.Orbit)
      return BuildOrbit(deltaSeconds, gunPivot, shot);

    var eye = _freePosition;
    var target = eye + GetLookDirection(_freeYaw, _freePitch) * 50f;
    return RayCamera.Perspective(eye, target, Vector3.UnitY, FovDegrees);
  }

  private void UpdateFreecam(RayGameContext ctx)
  {
    var delta = ctx.MouseDelta;
    _freeYaw -= delta.X * MouseSensitivity;
    _freePitch -= delta.Y * MouseSensitivity;
    _freePitch = Math.Clamp(_freePitch, -PitchLimit, PitchLimit);

    var move = Vector3.Zero;
    var forward = GetLookDirection(_freeYaw, _freePitch);
    var right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));

    if (ctx.IsKeyDown(KeyboardKey.W))
      move += forward;
    if (ctx.IsKeyDown(KeyboardKey.S))
      move -= forward;
    if (ctx.IsKeyDown(KeyboardKey.A))
      move -= right;
    if (ctx.IsKeyDown(KeyboardKey.D))
      move += right;

    if (move.LengthSquared() > 1e-8f)
    {
      move = Vector3.Normalize(move) * (FreeMoveSpeed * ctx.DeltaSeconds);
      _freePosition += move;
    }
  }

  private void UpdateOrbit(RayGameContext ctx, Vector3 gunPivot, ProjectileRun shot)
  {
    _orbitTarget = shot.Phase != ShotPhase.Ready
        ? shot.CurrentPosition + new Vector3(0f, 6f, 0f)
        : gunPivot + new Vector3(0f, 4f, 0f);

    var delta = ctx.MouseDelta;
    _orbitYaw -= delta.X * MouseSensitivity;
    _orbitPitch -= delta.Y * MouseSensitivity;
    _orbitPitch = Math.Clamp(_orbitPitch, -0.1f, PitchLimit);

    if (ctx.IsKeyDown(KeyboardKey.H))
      _orbitDistance = Math.Clamp(_orbitDistance - 900f * ctx.DeltaSeconds, MinOrbitDistance, MaxOrbitDistance);
    if (ctx.IsKeyDown(KeyboardKey.M))
      _orbitDistance = Math.Clamp(_orbitDistance + 900f * ctx.DeltaSeconds, MinOrbitDistance, MaxOrbitDistance);
  }

  private RayCamera BuildOrbit(float deltaSeconds, Vector3 gunPivot, ProjectileRun shot)
  {
    _orbitTarget = shot.Phase != ShotPhase.Ready
        ? shot.CurrentPosition + new Vector3(0f, 6f, 0f)
        : gunPivot + new Vector3(0f, 4f, 0f);

    if (!_orbitInitialized)
    {
      _smoothedOrbitTarget = _orbitTarget;
      _orbitInitialized = true;
    }
    else
    {
      var t = 1f - MathF.Exp(-OrbitSmoothRate * MathF.Max(deltaSeconds, 1e-4f));
      _smoothedOrbitTarget = Vector3.Lerp(_smoothedOrbitTarget, _orbitTarget, t);
    }

    var cosP = MathF.Cos(_orbitPitch);
    var offset = new Vector3(
      MathF.Sin(_orbitYaw) * cosP * _orbitDistance,
      MathF.Sin(_orbitPitch) * _orbitDistance,
      MathF.Cos(_orbitYaw) * cosP * _orbitDistance);

    var eye = _smoothedOrbitTarget + offset;
    return RayCamera.Perspective(eye, _smoothedOrbitTarget, Vector3.UnitY, FovDegrees);
  }

  private static Vector3 GetLookDirection(float yaw, float pitch)
  {
    var cosP = MathF.Cos(pitch);
    return Vector3.Normalize(new Vector3(MathF.Sin(yaw) * cosP, MathF.Sin(pitch), MathF.Cos(yaw) * cosP));
  }
}
