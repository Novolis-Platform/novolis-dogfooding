using System.Drawing;
using System.Numerics;
using Novolis.Raylib.Game;
using Novolis.Raylib.Interact;

namespace ArtillerySimulator.Game;

internal sealed class ArtillerySimulatorGame
{
  private static readonly Color Background = Color.FromArgb(255, 14, 18, 26);
  private static readonly Color TrailColor = Color.FromArgb(255, 200, 220, 160);
  private static readonly Color ImpactColor = Color.FromArgb(255, 255, 210, 120);

  private readonly TerrainWorld _terrain = new();
  private readonly GunModel _gun = new();
  private readonly ProjectileRun _shot = new();
  private readonly OverviewCamera _camera = new();

  private float _smoothedFps = 60f;
  private bool _fpsInit;
  private int _terrainSeed = 42;
  private bool _flatTerrain;

  public void Initialize(RayGameContext ctx)
  {
    _terrain.Rebuild(_terrainSeed, _flatTerrain);
    _shot.Reset();
  }

  public void Update(RayGameContext ctx)
  {
    HandleInput(ctx);

    if (_shot.Phase == ShotPhase.InFlight)
    {
      for (var i = 0; i < 4; i++)
        _shot.Advance(_terrain.Collision, _gun, _terrain.GunBaseline);
    }

    var dt = ctx.DeltaSeconds;
    if (dt > 1e-6f)
    {
      var instant = 1f / dt;
      _smoothedFps = _fpsInit ? _smoothedFps * 0.9f + instant * 0.1f : instant;
      _fpsInit = true;
    }

    ctx.Clear(Background);
    ctx.BeginWorld(_camera.Build());
    _terrain.Draw(ctx);
    _gun.Draw(ctx, _terrain.GunBaseline);
    DrawShot(ctx);
    ctx.EndWorld();

    SimulationHud.Draw(ctx, _gun, _terrain, _shot, _smoothedFps);
  }

  private void HandleInput(RayGameContext ctx)
  {
    if (ctx.IsKeyPressed(KeyboardKey.W))
      _gun.NudgeElevation(0.5f);
    if (ctx.IsKeyPressed(KeyboardKey.S))
      _gun.NudgeElevation(-0.5f);
    if (ctx.IsKeyPressed(KeyboardKey.A))
      _gun.NudgeAzimuth(-1f);
    if (ctx.IsKeyPressed(KeyboardKey.P))
      _gun.NudgeAzimuth(1f);

    if (ctx.IsKeyPressed(KeyboardKey.One))
      _gun.SetCharge(0);
    if (ctx.IsKeyPressed(KeyboardKey.Two))
      _gun.SetCharge(1);
    if (ctx.IsKeyPressed(KeyboardKey.Three))
      _gun.SetCharge(2);

    if (ctx.IsKeyPressed(KeyboardKey.D))
      _gun.ToggleDrag();

    if (ctx.IsKeyPressed(KeyboardKey.F))
    {
      _flatTerrain = !_flatTerrain;
      _terrain.Rebuild(_terrainSeed, _flatTerrain);
      _shot.Reset();
    }

    if (ctx.IsKeyPressed(KeyboardKey.R))
    {
      _terrainSeed = Random.Shared.Next();
      _terrain.Rebuild(_terrainSeed, _flatTerrain);
      _shot.Reset();
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

  private void DrawShot(RayGameContext ctx)
  {
    var trail = _shot.Trail;
    for (var i = 1; i < trail.Count; i++)
      ctx.DrawBolt(trail[i - 1], trail[i], TrailColor);

    if (_shot.Phase != ShotPhase.Ready)
      ctx.DrawGlowSphere(_shot.CurrentPosition, 0.12f, TrailColor);

    if (_shot.Impact is { } impact)
    {
      ctx.DrawGlowSphereWires(impact.Position, 0.35f, ImpactColor);
      var p = impact.Position;
      ctx.DrawBolt(p + new Vector3(-1f, 0f, 0f), p + new Vector3(1f, 0f, 0f), ImpactColor);
      ctx.DrawBolt(p + new Vector3(0f, 0f, -1f), p + new Vector3(0f, 0f, 1f), ImpactColor);
    }
  }
}
