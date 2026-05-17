using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using Novolis.Raylib.Game;
using Novolis.Raylib.Interact;
using Novolis.Raylib.Rendering;

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

    public void Update(RayGameContext ctx, PlayerController player, PlayerCombatState combat)
    {
        if (combat.IsDead)
            return;

        var dt = ctx.DeltaSeconds;
        var playerPos = player.Camera.Position;

        foreach (var enemy in _enemies)
        {
            if (!enemy.Alive)
                continue;

            enemy.MeleeCooldown = Math.Max(0f, enemy.MeleeCooldown - dt);

            var toPlayer = playerPos - enemy.Position;
            toPlayer.Y = 0f;
            var dist = toPlayer.Length();
            if (dist > 0.05f && dist < 12f)
            {
                var step = Vector3.Normalize(toPlayer) * (1.8f * dt);
                enemy.Position += step;
            }

            if (dist <= PlayerCombatState.EnemyMeleeRange && enemy.MeleeCooldown <= 0f)
            {
                combat.TakeDamage(PlayerCombatState.EnemyMeleeDamage);
                enemy.MeleeCooldown = PlayerCombatState.EnemyMeleeCooldown;
            }
        }

        var wantsFire = ctx.IsKeyPressed(KeyboardKey.Space)
            || ctx.IsMousePressed(MouseButton.Left)
            || ctx.IsMouseDown(MouseButton.Left);

        if (!wantsFire || !combat.TryConsumeShot())
            return;

        if (TryHit(player, combat))
            combat.RegisterHit();
    }

    public void Draw(RayGameContext ctx, Camera camera)
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

    private bool TryHit(PlayerController player, PlayerCombatState combat)
    {
        var ray = player.GetLookRay();
        var hitAny = false;

        foreach (var enemy in _enemies)
        {
            if (!enemy.Alive)
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
