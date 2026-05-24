using System.Drawing;
using System.Numerics;
using Novolis.Raylib.Game;
using Novolis.Raylib.Interact;
using Novolis.Raylib.Rendering;

namespace XFighter.Game;

internal sealed class XFighterGame
{
    private static readonly Color SpaceBlack = Color.FromArgb(255, 4, 6, 14);
    private static readonly Color LaserRed = Color.FromArgb(255, 255, 60, 80);
    private static readonly Color ExplosionCore = Color.FromArgb(255, 255, 220, 120);

    private readonly Random _rng = new(42);
    private readonly PlayerFlight _player = new();
    private readonly Starfield _starfield;
    private readonly CockpitHud _hud = new();
    private readonly HFighter[] _enemies;
    private readonly LaserBolt[] _bolts;
    private readonly Explosion[] _explosions;

    private float _fireCooldown;
    private float _spawnTimer;
    private int _score;
    private float _shield = 1f;
    public XFighterGame()
    {
        _starfield = new Starfield(420, _rng);
        _enemies = Enumerable.Range(0, 12).Select(_ => new HFighter()).ToArray();
        _bolts = Enumerable.Range(0, 48).Select(_ => new LaserBolt()).ToArray();
        _explosions = Enumerable.Range(0, 16).Select(_ => new Explosion()).ToArray();
    }

    public void Initialize(RayGameContext ctx)
    {
        _hud.Initialize(ctx);
        ctx.DisableCursor();
        SpawnWave();
    }

    public void Update(RayGameContext ctx)
    {
        if (ctx.IsKeyPressed(KeyboardKey.Escape))
            return;

        if (ctx.IsKeyPressed(KeyboardKey.R))
        {
            _score = 0;
            _shield = 1f;
            _player.Position = Vector3.Zero;
            _player.Speed = 22f;
            foreach (var e in _enemies)
                e.Active = false;
            SpawnWave();
        }

        _player.Update(ctx);
        UpdateCombat(ctx);
        UpdateEnemies(ctx);
        TrySpawn(ctx);

        ctx.Clear(SpaceBlack);
        var camera = _player.BuildCamera();
        ctx.BeginWorld(camera);
        _starfield.Draw(ctx, _player.Position);
        DrawWorld(ctx);
        ctx.EndWorld();
        _hud.Draw(ctx, _player, _score, CountActiveEnemies(), _shield);
    }

    private void DrawWorld(RayGameContext ctx)
    {
        foreach (var e in _enemies)
            e.Draw(ctx);

        foreach (var bolt in _bolts)
        {
            if (!bolt.Active)
                continue;
            var end = bolt.Position + bolt.Velocity * 0.08f;
            ctx.DrawLaserBolt(bolt.Position, end, LaserRed);
        }

        foreach (var ex in _explosions)
        {
            if (!ex.Active)
                continue;
            var t = 1f - ex.Life / ex.MaxLife;
            var r = 0.5f + t * 4f;
            ctx.DrawGlowSphere(ex.Position, r, ExplosionCore);
            ctx.DrawGlowSphereWires(ex.Position, r * 1.2f, LaserRed);
        }
    }

    private void UpdateCombat(RayGameContext ctx)
    {
        var dt = ctx.DeltaSeconds;
        _fireCooldown = Math.Max(0, _fireCooldown - dt);

        var firing = ctx.IsKeyDown(KeyboardKey.Space) || ctx.IsMouseDown(MouseButton.Left);
        if (firing && _fireCooldown <= 0)
        {
            _fireCooldown = 0.12f;
            FireBolt();
        }

        foreach (var bolt in _bolts)
        {
            if (!bolt.Active)
                continue;
            bolt.Position += bolt.Velocity * dt;
            bolt.Life -= dt;
            if (bolt.Life <= 0 || Vector3.Distance(bolt.Position, _player.Position) > 200f)
                bolt.Active = false;
        }

        foreach (var bolt in _bolts)
        {
            if (!bolt.Active)
                continue;
            var prev = bolt.Position - bolt.Velocity * dt;
            foreach (var enemy in _enemies)
            {
                if (!enemy.Active)
                    continue;
                if (!CombatSystem.SegmentHitsSphere(prev, bolt.Position, enemy.Position, enemy.HitRadius))
                    continue;

                bolt.Active = false;
                enemy.Health -= 0.55f;
                if (enemy.Health <= 0)
                {
                    enemy.Active = false;
                    _score += 100;
                    SpawnExplosion(enemy.Position);
                }

                break;
            }
        }

        foreach (var ex in _explosions)
        {
            if (!ex.Active)
                continue;
            ex.Life -= dt;
            if (ex.Life <= 0)
                ex.Active = false;
        }

        foreach (var enemy in _enemies)
        {
            if (!enemy.Active)
                continue;
            if (Vector3.Distance(enemy.Position, _player.Position) < 3.5f)
            {
                _shield -= dt * 0.35f;
                if (_shield <= 0)
                    _shield = 0;
            }
        }
    }

    private void UpdateEnemies(RayGameContext ctx)
    {
        foreach (var enemy in _enemies)
            enemy.Update(ctx.DeltaSeconds, _player.Position);
    }

    private void TrySpawn(RayGameContext ctx)
    {
        _spawnTimer -= ctx.DeltaSeconds;
        if (_spawnTimer > 0 || CountActiveEnemies() >= 6)
            return;

        _spawnTimer = 2.5f;
        foreach (var enemy in _enemies)
        {
            if (enemy.Active)
                continue;
            enemy.Spawn(_player.Position, _player.Forward, _rng);
            break;
        }
    }

    private void SpawnWave()
    {
        var spawned = 0;
        foreach (var enemy in _enemies)
        {
            if (spawned >= 4)
                break;
            enemy.Spawn(_player.Position, _player.Forward, _rng);
            spawned++;
        }

        _spawnTimer = 1.5f;
    }

    private void FireBolt()
    {
        foreach (var bolt in _bolts)
        {
            if (bolt.Active)
                continue;
            bolt.Active = true;
            bolt.Life = 2.5f;
            bolt.Position = _player.Position + _player.Forward * 2f;
            bolt.Velocity = _player.Forward * 120f;
            return;
        }
    }

    private void SpawnExplosion(Vector3 pos)
    {
        foreach (var ex in _explosions)
        {
            if (ex.Active)
                continue;
            ex.Active = true;
            ex.Position = pos;
            ex.MaxLife = 0.45f;
            ex.Life = ex.MaxLife;
            return;
        }
    }

    private int CountActiveEnemies()
    {
        var n = 0;
        foreach (var e in _enemies)
            if (e.Active)
                n++;
        return n;
    }
}
