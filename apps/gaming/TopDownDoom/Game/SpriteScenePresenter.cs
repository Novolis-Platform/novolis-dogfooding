using System.Numerics;
using Novolis.Math.Geometry;
using Novolis.Rendering.TwoD;
using TopDownDoom.Art;
using TopDownDoom.Design;

namespace TopDownDoom.Game;

internal sealed class SpriteScenePresenter(CharacterArtLibrary art)
{
    private readonly Dictionary<Monster, TwoDAnimatedSprite> _monsterSprites = new();
    private TwoDAnimatedSprite? _playerSprite;
    private float _shootDisplayTimer;

    public void Sync(TwoDScene scene, TopDownCombatWorld world, float dt)
    {
        _shootDisplayTimer = MathF.Max(0f, _shootDisplayTimer - dt);
        if (world.FireCooldown > world.ActiveWeapon.FireInterval - TimeSpan.FromMilliseconds(120))
        {
            _shootDisplayTimer = 0.18f;
        }

        SyncPlayer(scene, world);
        SyncMonsters(scene, world);
        SyncPickups(scene, world);
        SyncBarrels(scene, world);
        SyncProjectiles(scene, world);
    }

    public void Clear(TwoDScene scene)
    {
        foreach (var sprite in _monsterSprites.Values)
        {
            scene.AnimatedSprites.Remove(sprite);
        }

        _monsterSprites.Clear();
        if (_playerSprite is not null)
        {
            scene.AnimatedSprites.Remove(_playerSprite);
            _playerSprite = null;
        }

        scene.Sprites.RemoveAll(s => s.SortKey is >= 30 and < 200);
    }

    private void SyncPlayer(TwoDScene scene, TopDownCombatWorld world)
    {
        var clip = _shootDisplayTimer > 0f && art.Player.Shoot is not null
            ? art.Player.Shoot
            : art.Player.Walk;
        var moving = world.PlayerVelocity.LengthSquared() > 0.2f;
        if (!moving && _shootDisplayTimer <= 0f)
        {
            clip = art.Player.Walk;
        }

        _playerSprite ??= new TwoDAnimatedSprite { Clip = clip, Loop = true, SortKey = 120 };
        _playerSprite.Clip = clip;
        _playerSprite.Transform.Position = world.PlayerPosition;
        _playerSprite.Transform.RotationY = world.PlayerFacingRadians;
        ApplyScale(_playerSprite.Transform, clip.Sheet, art.Player.WorldHalfHeight);
        if (!scene.AnimatedSprites.Contains(_playerSprite))
        {
            scene.AnimatedSprites.Add(_playerSprite);
        }
    }

    private void SyncMonsters(TwoDScene scene, TopDownCombatWorld world)
    {
        var live = new HashSet<Monster>(world.Monsters);
        foreach (var pair in _monsterSprites.ToArray())
        {
            if (!live.Contains(pair.Key))
            {
                scene.AnimatedSprites.Remove(pair.Value);
                _monsterSprites.Remove(pair.Key);
            }
        }

        foreach (var monster in world.Monsters)
        {
            if (!_monsterSprites.TryGetValue(monster, out var sprite))
            {
                var set = art.ForRole(monster.Role);
                sprite = new TwoDAnimatedSprite { Clip = set.Walk, Loop = true, SortKey = 90 };
                _monsterSprites[monster] = sprite;
                scene.AnimatedSprites.Add(sprite);
            }

            var set2 = art.ForRole(monster.Role);
            sprite.Clip = set2.Walk;
            sprite.Transform.Position = monster.Position;
            sprite.Transform.RotationY = MathF.Atan2(
                world.PlayerPosition.Z - monster.Position.Z,
                world.PlayerPosition.X - monster.Position.X);
            ApplyScale(sprite.Transform, set2.Walk.Sheet, set2.WorldHalfHeight);
        }
    }

    private void SyncPickups(TwoDScene scene, TopDownCombatWorld world)
    {
        scene.Sprites.RemoveAll(s => s.SortKey is >= 40 and < 60);
        foreach (var pickup in world.Pickups)
        {
            var tex = pickup.Kind switch
            {
                PickupKind.Health => art.HealthIcon,
                PickupKind.Armor => art.ArmorIcon,
                PickupKind.Ammo => art.AmmoIcon,
                PickupKind.BlueKey => art.KeyIcon,
                PickupKind.Exit => art.ExitIcon,
                _ => art.AmmoIcon,
            };
            AddWorldSprite(scene, pickup.Position, tex, 0.35f, 45);
        }
    }

    private void SyncBarrels(TwoDScene scene, TopDownCombatWorld world)
    {
        scene.Sprites.RemoveAll(s => s.SortKey is >= 50 and < 70);
        foreach (var barrel in world.Barrels)
        {
            AddWorldSprite(scene, barrel.Position, art.BarrelIcon, 0.4f, 55);
        }
    }

    private void SyncProjectiles(TwoDScene scene, TopDownCombatWorld world)
    {
        scene.StaticPolygons.RemoveAll(p => p.SortKey is >= 70 and < 85);
        foreach (var shot in world.Projectiles)
        {
            var color = shot.FromPlayer ? new Rgba32(255, 240, 120) : new Rgba32(255, 80, 80);
            var poly = TwoDScenePrimitives.Rectangle(
                shot.Position.X - 0.08f,
                shot.Position.Z - 0.08f,
                shot.Position.X + 0.08f,
                shot.Position.Z + 0.08f);
            scene.StaticPolygons.Add(new TwoDStaticPolygon(poly, color)
            {
                DrawFilled = true,
                SortKey = 80,
            });
        }
    }

    private static void AddWorldSprite(TwoDScene scene, Vector3 pos, TwoDTextureId tex, float halfSize, int sort)
    {
        scene.Sprites.Add(new TwoDSpriteInstance
        {
            Texture = tex,
            SortKey = sort,
            Transform =
            {
                Position = pos,
                Scale = new Vector3(halfSize, 1f, halfSize),
            },
        });
    }

    private static void ApplyScale(TwoDTransform transform, TwoDSpriteSheet sheet, float worldHalfHeight)
    {
        var aspect = sheet.FrameWidth / (float)Math.Max(1, sheet.FrameHeight);
        transform.Scale = new Vector3(worldHalfHeight * aspect, 1f, worldHalfHeight);
    }
}
