using System.Numerics;
using Novolis.Math.Geometry;
using TopDownDoom.Design;

namespace TopDownDoom.Game;

internal sealed class TopDownCombatWorld
{
    public Vector3 PlayerPosition { get; set; }
    public Vector2 PlayerVelocity { get; set; }
    public float PlayerFacingRadians { get; set; }
    public int Health { get; set; } = 100;
    public int Armor { get; set; }
    public int Ammo { get; set; } = 48;
    public WeaponSpec ActiveWeapon { get; set; } = WeaponCatalog.Pistol;
    public TimeSpan FireCooldown { get; set; }
    public TimeSpan DashCooldown { get; set; }
    public bool HasBlueKey { get; set; }
    public bool ExitUnlocked { get; set; }
    public bool ClosetAmbushTriggered { get; set; }
    public bool FoundShotgun { get; set; }

    public readonly CombatJuice Juice = new();
    public readonly List<Projectile> Projectiles = [];
    public readonly List<Monster> Monsters = [];
    public readonly List<Pickup> Pickups = [];
    public readonly List<ExplosiveBarrel> Barrels = [];

    public void Tick(float dt, Vector2 moveInput, Vector2 aimWorld, bool shootHeld, bool dashPressed)
    {
        FireCooldown -= TimeSpan.FromSeconds(dt);
        DashCooldown -= TimeSpan.FromSeconds(dt);

        if (dashPressed && DashCooldown <= TimeSpan.Zero && moveInput.LengthSquared() > 0.01f)
        {
            var dash = Vector2.Normalize(moveInput) * 4.2f;
            PlayerVelocity += dash;
            DashCooldown = TimeSpan.FromSeconds(2.8);
        }

        PlayerVelocity = DoomMovement.Integrate(PlayerVelocity, moveInput, dt);

        if (aimWorld.LengthSquared() > 0.01f)
        {
            PlayerFacingRadians = MathF.Atan2(aimWorld.Y, aimWorld.X);
        }

        if (shootHeld && FireCooldown <= TimeSpan.Zero && Ammo > 0)
        {
            FireWeapon();
        }

        TickProjectiles(dt);
        TickMonsters(dt);
        TickBarrels(dt);
        ResolvePickups();
        Juice.Tick(dt);
    }

    private void FireWeapon()
    {
        var weapon = ActiveWeapon;
        FireCooldown = weapon.FireInterval;
        Ammo = Math.Max(0, Ammo - 1);

        for (var i = 0; i < weapon.PelletCount; i++)
        {
            var spread = weapon.SpreadDegrees * (MathF.PI / 180f);
            var angle = PlayerFacingRadians + Random.Shared.NextSingle() * spread - spread * 0.5f;
            var dir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            Projectiles.Add(new Projectile(
                PlayerPosition + new Vector3(dir.X * 0.4f, 0f, dir.Y * 0.4f),
                new Vector3(dir.X * weapon.ProjectileSpeed, 0f, dir.Y * weapon.ProjectileSpeed),
                weapon.Damage,
                weapon.SplashRadius,
                weapon.CausesSelfDamage,
                true));
        }
    }

    private void TickProjectiles(float dt)
    {
        for (var i = Projectiles.Count - 1; i >= 0; i--)
        {
            var p = Projectiles[i];
            p.Position += p.Velocity * dt;
            p.Life -= dt;
            if (p.Life <= 0f)
            {
                Projectiles.RemoveAt(i);
                continue;
            }

            if (p.SplashRadius > 0f)
            {
                var detonate = false;
                for (var m = 0; m < Monsters.Count; m++)
                {
                    if (DistanceXZ(p.Position, Monsters[m].Position) < p.SplashRadius * 0.45f)
                    {
                        detonate = true;
                        break;
                    }
                }

                for (var b = 0; b < Barrels.Count; b++)
                {
                    if (DistanceXZ(p.Position, Barrels[b].Position) < 0.6f)
                    {
                        detonate = true;
                        break;
                    }
                }

                if (detonate)
                {
                    TrySplash(p, includePlayer: p.CausesSelfDamage);
                    Projectiles.RemoveAt(i);
                }
            }
            else
            {
                for (var m = Monsters.Count - 1; m >= 0; m--)
                {
                    if (DistanceXZ(p.Position, Monsters[m].Position) < 0.45f)
                    {
                        DamageMonster(m, p.Damage);
                        Projectiles.RemoveAt(i);
                        break;
                    }
                }
            }
        }
    }

    private void TrySplash(Projectile p, bool includePlayer)
    {
        var hit = false;
        for (var m = Monsters.Count - 1; m >= 0; m--)
        {
            if (DistanceXZ(p.Position, Monsters[m].Position) <= p.SplashRadius)
            {
                DamageMonster(m, p.Damage);
                hit = true;
            }
        }

        for (var b = Barrels.Count - 1; b >= 0; b--)
        {
            if (DistanceXZ(p.Position, Barrels[b].Position) <= p.SplashRadius)
            {
                DetonateBarrel(b);
                hit = true;
            }
        }

        if (includePlayer && DistanceXZ(p.Position, PlayerPosition) <= p.SplashRadius)
        {
            ApplyPlayerDamage(p.Damage / 2);
        }

        if (hit || p.Life <= 0f)
        {
            Projectiles.Remove(p);
        }
    }

    private void TickMonsters(float dt)
    {
        for (var i = Monsters.Count - 1; i >= 0; i--)
        {
            var monster = Monsters[i];
            if (monster.Health <= 0)
            {
                Monsters.RemoveAt(i);
                continue;
            }

            var toPlayer = PlayerPosition - monster.Position;
            var flat = new Vector2(toPlayer.X, toPlayer.Z);
            var dist = flat.Length();
            if (dist > 0.05f)
            {
                flat /= dist;
            }

            monster.FireCooldown -= TimeSpan.FromSeconds(dt);
            switch (monster.Role)
            {
                case MonsterRole.Fodder:
                    monster.Position += new Vector3(flat.X * 3.2f * dt, 0f, flat.Y * 3.2f * dt);
                    break;
                case MonsterRole.Projectile:
                    monster.Position += new Vector3(flat.X * 2.4f * dt, 0f, flat.Y * 2.4f * dt);
                    if (dist < 14f && monster.FireCooldown <= TimeSpan.Zero)
                    {
                        ShootFrom(monster, flat, 10f, 12);
                        monster.FireCooldown = TimeSpan.FromSeconds(1.4);
                    }
                    break;
                case MonsterRole.Charger:
                    monster.Position += new Vector3(flat.X * 6.5f * dt, 0f, flat.Y * 6.5f * dt);
                    break;
                case MonsterRole.Hitscan:
                    if (dist < 16f && monster.FireCooldown <= TimeSpan.Zero && HasLineOfSight(monster.Position))
                    {
                        ApplyPlayerDamage(6);
                        monster.FireCooldown = TimeSpan.FromSeconds(0.9);
                    }
                    break;
                case MonsterRole.Bruiser:
                    monster.Position += new Vector3(flat.X * 2f * dt, 0f, flat.Y * 2f * dt);
                    break;
            }
        }
    }

    private void ShootFrom(Monster monster, Vector2 dir, float speed, int damage)
    {
        Projectiles.Add(new Projectile(
            monster.Position,
            new Vector3(dir.X * speed, 0f, dir.Y * speed),
            damage,
            0f,
            false,
            false));
    }

    private bool HasLineOfSight(Vector3 from) =>
        DistanceXZ(from, PlayerPosition) < 18f;

    private void TickBarrels(float dt)
    {
        for (var i = Barrels.Count - 1; i >= 0; i--)
        {
            if (Barrels[i].Fuse <= 0f)
            {
                continue;
            }

            Barrels[i].Fuse -= dt;
            if (Barrels[i].Fuse <= 0f)
            {
                DetonateBarrel(i);
            }
        }
    }

    private void DetonateBarrel(int index)
    {
        if (index < 0 || index >= Barrels.Count)
        {
            return;
        }

        var barrel = Barrels[index];
        Barrels.RemoveAt(index);
        Projectiles.Add(new Projectile(barrel.Position, Vector3.Zero, 55, 2.4f, true, false)
        {
            Life = 0.01f,
        });
    }

    private void DamageMonster(int index, int damage)
    {
        Monsters[index].Health -= damage;
    }

    private void ResolvePickups()
    {
        for (var i = Pickups.Count - 1; i >= 0; i--)
        {
            if (DistanceXZ(PlayerPosition, Pickups[i].Position) > 0.7f)
            {
                continue;
            }

            switch (Pickups[i].Kind)
            {
                case PickupKind.Health:
                    Health = Math.Min(100, Health + 25);
                    break;
                case PickupKind.Armor:
                    Armor = Math.Min(100, Armor + 20);
                    break;
                case PickupKind.Ammo:
                    Ammo += 24;
                    break;
                case PickupKind.BlueKey:
                    HasBlueKey = true;
                    TriggerClosetAmbush();
                    break;
                case PickupKind.Exit:
                    if (HasBlueKey)
                    {
                        ExitUnlocked = true;
                    }
                    break;
            }

            Pickups.RemoveAt(i);
        }
    }

    public void TriggerClosetAmbush()
    {
        if (ClosetAmbushTriggered)
        {
            return;
        }

        ClosetAmbushTriggered = true;
        SpawnMonster(MonsterRole.Fodder, 28f, 8f);
        SpawnMonster(MonsterRole.Fodder, 30f, 10f);
        SpawnMonster(MonsterRole.Projectile, 32f, 6f);
        SpawnMonster(MonsterRole.Charger, 34f, 12f);
        SpawnMonster(MonsterRole.Bruiser, 36f, 9f);
    }

    public void SpawnMonster(MonsterRole role, float x, float z)
    {
        var hp = role switch
        {
            MonsterRole.Bruiser => 80,
            MonsterRole.Charger => 35,
            MonsterRole.Projectile => 28,
            _ => 20,
        };
        Monsters.Add(new Monster(role, Vector3PlanarExtensions.Xz(x, z), hp));
    }

    private void ApplyPlayerDamage(int amount)
    {
        var absorbed = Math.Min(Armor, amount / 2);
        Armor -= absorbed;
        Health -= amount - absorbed;
        Health = Math.Max(0, Health);
    }

    private static float DistanceXZ(Vector3 a, Vector3 b)
    {
        var dx = a.X - b.X;
        var dz = a.Z - b.Z;
        return MathF.Sqrt(dx * dx + dz * dz);
    }
}

internal sealed class Projectile(
    Vector3 position,
    Vector3 velocity,
    int damage,
    float splashRadius,
    bool causesSelfDamage,
    bool fromPlayer)
{
    public Vector3 Position = position;
    public Vector3 Velocity = velocity;
    public int Damage = damage;
    public float SplashRadius = splashRadius;
    public bool CausesSelfDamage = causesSelfDamage;
    public bool FromPlayer = fromPlayer;
    public float Life = 4f;
}

internal sealed class Monster(MonsterRole role, Vector3 position, int health)
{
    public MonsterRole Role = role;
    public Vector3 Position = position;
    public int Health = health;
    public TimeSpan FireCooldown;
}

internal enum PickupKind { Health, Armor, Ammo, BlueKey, Exit }

internal sealed class Pickup(PickupKind kind, Vector3 position)
{
    public PickupKind Kind = kind;
    public Vector3 Position = position;
}

internal sealed class ExplosiveBarrel(Vector3 position)
{
    public Vector3 Position = position;
    public float Fuse;
}
