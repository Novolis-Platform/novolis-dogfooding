using System.Drawing;
using System.Numerics;
using Novolis.Raylib.Game;
using Novolis.Raylib.Interact;

namespace ArtillerySimulator.Game;

internal sealed class ArtillerySimulatorGame
{
  private const int PhysicsStepsPerFrame = 32;
  private const int CamToggleKey = 67;
  private const int KeyQ = 81;
  private const int KeyE = 69;
  private const float AimDegreesPerSecond = 28f;

  private static readonly Color Background = Color.FromArgb(255, 14, 18, 26);
  private static readonly Color TrailColor = Color.FromArgb(255, 200, 220, 160);
  private static readonly Color PreviewColor = Color.FromArgb(120, 140, 160, 90);
  private static readonly Color ImpactColor = Color.FromArgb(255, 255, 210, 120);

  private readonly TerrainWorld _terrain = new();
  private readonly GunModel _gun = new();
  private readonly ProjectileRun _shot = new();
  private readonly ArtilleryCamera _camera = new();

  private float _smoothedFps = 60f;
  private bool _fpsInit;
  private int _terrainSeed = 42;
  private bool _flatTerrain;
  private IReadOnlyList<Vector3> _aimPreview = [];

  public void Initialize(RayGameContext ctx)
  {
    ctx.DisableCursor();
    _terrain.Rebuild(_terrainSeed, _flatTerrain);
    _shot.Reset();
    _camera.SnapToGun(_terrain.GunBaseline, _gun.BarrelDirection());
    RebuildAimPreview();
  }

  public void Update(RayGameContext ctx)
  {
    HandleInput(ctx);

    if (_shot.Phase == ShotPhase.InFlight)
    {
      _shot.AdvanceWithBudget(
        _terrain,
        _gun,
        _terrain.GunBaseline,
        PhysicsStepsPerFrame);
    }

    if (_shot.Phase == ShotPhase.Ready)
      RebuildAimPreview();

    var dt = ctx.DeltaSeconds;
    if (dt > 1e-6f)
    {
      var instant = 1f / dt;
      _smoothedFps = _fpsInit ? _smoothedFps * 0.9f + instant * 0.1f : instant;
      _fpsInit = true;
    }

    _camera.Update(ctx, _terrain.GunBaseline, _shot);

    ctx.Clear(Background);
    var barrelDir = _gun.BarrelDirection();
    ctx.BeginWorld(_camera.Build(dt, _terrain.GunBaseline, barrelDir, _shot));
    _terrain.Draw(ctx);
    DrawAimPreview(ctx);
    _gun.Draw(ctx, _terrain.GunBaseline, showPivotGlow: _shot.Phase == ShotPhase.Ready);
    DrawShot(ctx);
    ctx.EndWorld();

    SimulationHud.Draw(ctx, _gun, _terrain, _shot, _camera, _smoothedFps);
  }

  private void RebuildAimPreview()
  {
    var muzzle = _gun.MuzzlePosition(_terrain.GunBaseline);
    _aimPreview = BallisticArcPreview.Build(_gun, _terrain, muzzle);
  }

  private void HandleInput(RayGameContext ctx)
  {
    var aimStep = AimDegreesPerSecond * ctx.DeltaSeconds;

    if (ctx.IsKeyDown(KeyboardKey.LeftShift))
      _gun.NudgeElevation(aimStep);
    if (ctx.IsKeyDown(KeyboardKey.LeftControl))
      _gun.NudgeElevation(-aimStep);

    if (ctx.IsKeyDown((KeyboardKey)KeyQ))
      _gun.NudgeAzimuth(-aimStep);
    if (ctx.IsKeyDown((KeyboardKey)KeyE))
      _gun.NudgeAzimuth(aimStep);

    if (ctx.IsKeyPressed(KeyboardKey.One))
      _gun.SetCharge(0);
    if (ctx.IsKeyPressed(KeyboardKey.Two))
      _gun.SetCharge(1);
    if (ctx.IsKeyPressed(KeyboardKey.Three))
      _gun.SetCharge(2);

    if (ctx.IsKeyPressed(KeyboardKey.D))
      _gun.ToggleDrag();

    if (ctx.IsKeyPressed((KeyboardKey)CamToggleKey))
      _camera.ToggleMode();

    if (ctx.IsKeyPressed(KeyboardKey.F))
    {
      _flatTerrain = !_flatTerrain;
      _terrain.Rebuild(_terrainSeed, _flatTerrain);
      _shot.Reset();
      _camera.SnapToGun(_terrain.GunBaseline, _gun.BarrelDirection());
    }

    if (ctx.IsKeyPressed(KeyboardKey.R))
    {
      _terrainSeed = Random.Shared.Next();
      _terrain.Rebuild(_terrainSeed, _flatTerrain);
      _shot.Reset();
      _camera.SnapToGun(_terrain.GunBaseline, _gun.BarrelDirection());
    }

    if (ctx.IsKeyPressed(KeyboardKey.Space) && _shot.Phase != ShotPhase.InFlight)
    {
      var muzzle = _gun.MuzzlePosition(_terrain.GunBaseline);
      var state = _gun.CreateProjectileState(muzzle, _gun.BarrelDirection());
      _shot.Begin(state);
      if (_flatTerrain && !_gun.DragEnabled)
        LogVacuumFlatSanity(state.Velocity);
    }
  }

  private static void LogVacuumFlatSanity(Vector3 velocity)
  {
    var vx = velocity.X;
    var vy = velocity.Y;
    var g = 9.80665;
    var expected = 2.0 * vx * vy / g;
    Console.WriteLine($"[ArtillerySimulator] vacuum flat expected range ~ {expected:F1} m (2*vx*vy/g)");
  }

  private void DrawAimPreview(RayGameContext ctx)
  {
    if (_shot.Phase != ShotPhase.Ready || _aimPreview.Count < 2)
      return;

    for (var i = 1; i < _aimPreview.Count; i++)
      ctx.DrawBolt(_aimPreview[i - 1], _aimPreview[i], PreviewColor);
  }

  private void DrawShot(RayGameContext ctx)
  {
    var trail = _shot.Trail;
    for (var i = 1; i < trail.Count; i++)
      ctx.DrawBolt(trail[i - 1], trail[i], TrailColor);

    if (_shot.Phase != ShotPhase.Ready)
      ctx.DrawGlowSphere(_shot.CurrentPosition, GunModel.MuzzleRadius, TrailColor);

    if (_shot.Impact is { } impact)
    {
      ctx.DrawGlowSphereWires(impact.Position, 8f, ImpactColor);
      var p = impact.Position;
      ctx.DrawBolt(p + new Vector3(-12f, 0f, 0f), p + new Vector3(12f, 0f, 0f), ImpactColor);
      ctx.DrawBolt(p + new Vector3(0f, 0f, -12f), p + new Vector3(0f, 0f, 12f), ImpactColor);
    }
  }
}
