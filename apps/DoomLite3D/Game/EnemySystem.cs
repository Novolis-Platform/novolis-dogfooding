using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using Novolis.Math.Geometry;
using Novolis.Physics.Abstractions;
using Novolis.Physics.Collision.Simple;
using Novolis.Raylib.Game;
using Novolis.Raylib.Interact;
using Novolis.Raylib.Rendering;
using RayCamera = Novolis.Raylib.Rendering.Camera;

namespace DoomLite3D.Game;

internal sealed class Enemy
{
    public Vector3 Position;
    public bool Alive = true;
    public int SpriteIndex;
    public float HitRadius = 1.1f;
    public float MeleeCooldown;
}

internal sealed class EnemySystem
{
    private const float BillboardSize = 2.2f;
    private const float MaxShootRange = 18f;
    private const float ChaseRange = 12f;
    private const float ChaseSpeed = 1.8f;
    private const float EnemyRadius = 0.45f;

    private readonly List<Enemy> _enemies = [];
    private Texture[] _sprites = [];
    private bool _loggedMissingAssets;

    public IReadOnlyList<Enemy> Enemies => _enemies;

    public void Initialize(RayGameContext ctx)
    {
        _sprites =
        [
            TryLoad(ctx, "enemies", "imp.png"),
            TryLoad(ctx, "enemies", "demon.png"),
        ];

        if (!_loggedMissingAssets)
        {
            _loggedMissingAssets = true;
            for (var i = 0; i < _sprites.Length; i++)
            {
                if (!_sprites[i].IsValid)
                    Debug.WriteLine($"DoomLite3D: missing or invalid texture for enemy sprite index {i}");
            }
        }
    }

    public void Reset(LevelMap level)
    {
        _enemies.Clear();
        foreach (var spawn in level.Enemies)
        {
            var world = level.CellToWorld(spawn.Cell.X, spawn.Cell.Y, 0.9f);
            _enemies.Add(new Enemy
            {
                Position = world,
                SpriteIndex = spawn.SpriteIndex,
                Alive = true,
            });
        }
    }

    public void Update(
        RayGameContext ctx,
        LevelMap level,
        IStaticWorld? physicsWorld,
        PlayerController player,
        PlayerCombatState combat)
    {
        if (combat.IsDead)
            return;

        var dt = ctx.DeltaSeconds;
        var playerPos = player.Camera.Position;
        var playerXZ = new Vector2(playerPos.X, playerPos.Z);

        foreach (var enemy in _enemies)
        {
            if (!enemy.Alive)
                continue;

            enemy.MeleeCooldown = Math.Max(0f, enemy.MeleeCooldown - dt);

            var enemyXZ = new Vector2(enemy.Position.X, enemy.Position.Z);
            var toPlayer = playerXZ - enemyXZ;
            var dist = toPlayer.Length();
            var hasLos = GridCollision2D.HasLineOfSight(level.Walls, enemyXZ, playerXZ, LevelMap.CellSize);

            if (hasLos && dist > 0.05f && dist < ChaseRange)
            {
                var step = Vector2.Normalize(toPlayer) * (ChaseSpeed * dt);
                var moved = TryMoveEnemy(level, physicsWorld, enemy, enemyXZ, step);
                enemy.Position = new Vector3(moved.X, enemy.Position.Y, moved.Y);
            }

            if (hasLos && dist <= PlayerCombatState.EnemyMeleeRange && enemy.MeleeCooldown <= 0f)
            {
                combat.TakeDamage(PlayerCombatState.EnemyMeleeDamage);
                enemy.MeleeCooldown = PlayerCombatState.EnemyMeleeCooldown;
            }
        }

        var wantsFire = ctx.IsMousePressed(MouseButton.Left)
            || ctx.IsMouseDown(MouseButton.Left)
            || ctx.IsKeyPressed(KeyboardKey.LeftControl)
            || ctx.IsKeyDown(KeyboardKey.LeftControl);

        if (!wantsFire || !combat.TryConsumeShot())
            return;

        if (TryHit(level, player, combat))
            combat.RegisterHit();
    }

    private Vector2 TryMoveEnemy(
        LevelMap level,
        IStaticWorld? physicsWorld,
        Enemy self,
        Vector2 position,
        Vector2 delta)
    {
        if (delta.LengthSquared() < 1e-12f)
            return position;

        var next = physicsWorld is null
            ? GridCollision2D.TryMove(level.Walls, position, delta, EnemyRadius, LevelMap.CellSize)
            : GridPhysicsMovement.TryMove(physicsWorld, position, delta, EnemyRadius, centerY: 0.9);

        if (!OverlapsOtherEnemy(self, next))
            return next;

        return position;
    }

    private bool OverlapsOtherEnemy(Enemy self, Vector2 position)
    {
        var minDist = EnemyRadius * 2f;
        var minDistSq = minDist * minDist;

        foreach (var other in _enemies)
        {
            if (!other.Alive || ReferenceEquals(other, self))
                continue;

            var otherXZ = new Vector2(other.Position.X, other.Position.Z);
            if (Vector2.DistanceSquared(position, otherXZ) < minDistSq)
                return true;
        }

        return false;
    }

    public void Draw(RayGameContext ctx, RayCamera camera)
    {
        foreach (var enemy in _enemies.OrderByDescending(e => e.Position.Z))
        {
            if (!enemy.Alive)
                continue;

            var tex = _sprites[enemy.SpriteIndex % _sprites.Length];
            if (!tex.IsValid)
            {
                ctx.DrawGlowSphere(enemy.Position, 0.7f, Color.FromArgb(255, 200, 60, 60));
                continue;
            }

            var source = new RectangleF(0, 0, tex.Width, tex.Height);
            ctx.DrawBillboardPro(camera, tex, source, enemy.Position, new Vector2(BillboardSize, BillboardSize), Color.White);
        }
    }

    public int CountAlive() => _enemies.Count(e => e.Alive);

    private bool TryHit(LevelMap level, PlayerController player, PlayerCombatState combat)
    {
        var ray = player.GetLookRay();
        var originXZ = new Vector2(ray.Origin.X, ray.Origin.Z);
        var hitAny = false;

        foreach (var enemy in _enemies)
        {
            if (!enemy.Alive)
                continue;

            var targetXZ = new Vector2(enemy.Position.X, enemy.Position.Z);
            if (!GridCollision2D.HasLineOfSight(level.Walls, originXZ, targetXZ, LevelMap.CellSize))
                continue;

            if (!CombatRaycast.TryHitEnemyXZ(
                    ray.Origin,
                    ray.Direction,
                    enemy.Position,
                    MaxShootRange,
                    enemy.HitRadius,
                    out _))
                continue;

            enemy.Alive = false;
            hitAny = true;
            combat.LastShotHit = true;
        }

        return hitAny;
    }

    private static Texture TryLoad(RayGameContext ctx, string folder, string file)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", folder, file);
        if (!File.Exists(path))
        {
            Debug.WriteLine($"DoomLite3D: asset not found: {path}");
            return default;
        }

        var tex = ctx.LoadTexture(path);
        return ctx.IsTextureValid(tex) ? tex : default;
    }
}
