using Novolis.Math.Geometry;
using Novolis.Rendering.TwoD;

namespace TopDownDoom.Art;

/// <summary>Built-in Doom-flavored sprites — no downloads required.</summary>
internal static class ProceduralDoomSprites
{
    public static CharacterAnimationSet CreateMarine(TwoDTextureRegistry registry) =>
        CreateArchetype(registry, DoomArchetype.Marine, 0.52f);

    public static CharacterAnimationSet CreateZombie(TwoDTextureRegistry registry) =>
        CreateArchetype(registry, DoomArchetype.Zombie, 0.5f);

    public static CharacterAnimationSet CreateImp(TwoDTextureRegistry registry) =>
        CreateArchetype(registry, DoomArchetype.Imp, 0.44f);

    public static CharacterAnimationSet CreateBruiser(TwoDTextureRegistry registry) =>
        CreateArchetype(registry, DoomArchetype.Pinky, 0.72f);

    public static TwoDAnimationClip CreateExplosionClip(TwoDTextureRegistry registry)
    {
        const int fw = 32;
        const int fh = 32;
        const int frames = 8;
        var atlas = new Rgba32[fw * frames * fh];
        for (var f = 0; f < frames; f++)
        {
            var t = f / (float)(frames - 1);
            var radius = 4f + t * 12f;
            var alpha = (byte)(255 * (1f - t * 0.85f));
            var core = new Rgba32(255, 240, 120, alpha);
            var outer = new Rgba32(255, 90, 30, (byte)(alpha * 0.7f));
            for (var y = 0; y < fh; y++)
            {
                for (var x = 0; x < fw; x++)
                {
                    var ox = f * fw + x;
                    var dx = x - 16f;
                    var dy = y - 16f;
                    var d = MathF.Sqrt(dx * dx + dy * dy);
                    if (d <= radius * 0.45f)
                    {
                        atlas[y * (fw * frames) + ox] = core;
                    }
                    else if (d <= radius)
                    {
                        atlas[y * (fw * frames) + ox] = outer;
                    }
                }
            }
        }

        var id = registry.Register(atlas, fw * frames, fh, "procedural-explosion");
        var sheet = new TwoDSpriteSheet(id, fw, fh, fw * frames, fh);
        return new TwoDAnimationClip(sheet, [0, 1, 2, 3, 4, 5, 6, 7], 18f);
    }

    public static TwoDTextureId CreatePickupIcon(TwoDTextureRegistry registry, PickupArtKind kind)
    {
        const int size = 32;
        var pixels = new Rgba32[size * size];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                pixels[y * size + x] = kind switch
                {
                    PickupArtKind.Health => PixelMedkit(x, y, size),
                    PickupArtKind.Armor => PixelArmor(x, y, size),
                    PickupArtKind.Ammo => PixelAmmoBox(x, y, size),
                    PickupArtKind.BlueKey => PixelKey(x, y, size),
                    PickupArtKind.Exit => PixelExitPad(x, y, size),
                    PickupArtKind.Barrel => PixelBarrel(x, y, size),
                    _ => default,
                };
            }
        }

        return registry.Register(pixels, size, size, kind.ToString());
    }

    private static CharacterAnimationSet CreateArchetype(
        TwoDTextureRegistry registry,
        DoomArchetype archetype,
        float worldHalfHeight)
    {
        const int fw = 40;
        const int fh = 40;
        const int frames = 8;
        var atlas = new Rgba32[fw * frames * fh];
        for (var f = 0; f < frames; f++)
        {
            var bob = MathF.Sin(f * MathF.PI * 0.5f) * 1.5f;
            DrawFrame(atlas, fw, fh, f, archetype, bob);
        }

        var id = registry.Register(atlas, fw * frames, fh, $"procedural-{archetype}");
        var sheet = new TwoDSpriteSheet(id, fw, fh, fw * frames, fh);
        var walk = new TwoDAnimationClip(sheet, [0, 1, 2, 3, 4, 5, 6, 7], 11f);
        var shoot = new TwoDAnimationClip(sheet, [2, 3, 4, 3, 2], 16f);
        return new CharacterAnimationSet(walk, worldHalfHeight, shoot);
    }

    private static void DrawFrame(Rgba32[] atlas, int fw, int fh, int frame, DoomArchetype archetype, float bob)
    {
        var ox = frame * fw;
        var palette = PaletteFor(archetype);
        var wide = archetype == DoomArchetype.Pinky ? 1.25f : 1f;

        FillEllipse(atlas, fw, fh, ox, 20, 22 + bob, 11 * wide, 13, palette.Body);
        FillEllipse(atlas, fw, fh, ox, 20, 12 + bob, 7 * wide, 8, palette.Skin);
        FillEllipse(atlas, fw, fh, ox, 20, 8 + bob, 5 * wide, 5, palette.Highlight);

        switch (archetype)
        {
            case DoomArchetype.Marine:
                FillRect(atlas, fw, fh, ox, 28, (int)(16 + bob), 10, 4, palette.Gun);
                FillRect(atlas, fw, fh, ox, 14, (int)(10 + bob), 3, 6, palette.Accent);
                break;
            case DoomArchetype.Zombie:
                FillRect(atlas, fw, fh, ox, 12, (int)(18 + bob), 4, 8, palette.Gore);
                FillRect(atlas, fw, fh, ox, 26, (int)(20 + bob), 6, 3, palette.Gore);
                break;
            case DoomArchetype.Imp:
                Set(atlas, fw, fh, ox, 16, (int)(6 + bob), palette.Horn);
                Set(atlas, fw, fh, ox, 24, (int)(6 + bob), palette.Horn);
                FillEllipse(atlas, fw, fh, ox, 30, 24 + bob, 4, 4, palette.Accent);
                break;
            case DoomArchetype.Pinky:
                FillEllipse(atlas, fw, fh, ox, 12, 26 + bob, 6, 5, palette.Gore);
                FillEllipse(atlas, fw, fh, ox, 28, 26 + bob, 6, 5, palette.Gore);
                break;
        }

        if (frame is 3 or 4)
        {
            FillRect(atlas, fw, fh, ox, 32, (int)(14 + bob), 12, 3, new Rgba32(255, 220, 80));
        }
    }

    private static (Rgba32 Body, Rgba32 Skin, Rgba32 Highlight, Rgba32 Gun, Rgba32 Gore, Rgba32 Accent, Rgba32 Horn) PaletteFor(
        DoomArchetype archetype) =>
        archetype switch
        {
            DoomArchetype.Marine => (
                new Rgba32(48, 92, 56),
                new Rgba32(210, 170, 120),
                new Rgba32(70, 120, 80),
                new Rgba32(60, 60, 70),
                new Rgba32(120, 20, 20),
                new Rgba32(90, 140, 200),
                new Rgba32(40, 40, 40)),
            DoomArchetype.Zombie => (
                new Rgba32(70, 110, 50),
                new Rgba32(140, 150, 90),
                new Rgba32(90, 130, 60),
                new Rgba32(80, 60, 50),
                new Rgba32(140, 30, 30),
                new Rgba32(50, 70, 40),
                new Rgba32(60, 50, 40)),
            DoomArchetype.Imp => (
                new Rgba32(160, 45, 55),
                new Rgba32(220, 80, 70),
                new Rgba32(255, 120, 60),
                new Rgba32(80, 30, 30),
                new Rgba32(100, 20, 20),
                new Rgba32(255, 180, 40),
                new Rgba32(40, 20, 20)),
            DoomArchetype.Pinky => (
                new Rgba32(150, 60, 90),
                new Rgba32(200, 100, 120),
                new Rgba32(220, 130, 150),
                new Rgba32(100, 40, 60),
                new Rgba32(180, 40, 50),
                new Rgba32(255, 100, 140),
                new Rgba32(80, 30, 40)),
            _ => (
                new Rgba32(120, 120, 120),
                new Rgba32(180, 180, 180),
                new Rgba32(220, 220, 220),
                new Rgba32(60, 60, 60),
                new Rgba32(120, 20, 20),
                new Rgba32(80, 80, 200),
                new Rgba32(40, 40, 40)),
        };

    private static Rgba32 PixelMedkit(int x, int y, int size)
    {
        if (x is >= 10 and <= 21 && y is >= 8 and <= 23)
        {
            return new Rgba32(200, 40, 40);
        }

        if ((x is 14 or 15 or 16 or 17) && y is >= 10 and <= 21)
        {
            return new Rgba32(240, 240, 240);
        }

        if ((y is 14 or 15 or 16 or 17) && x is >= 12 and <= 19)
        {
            return new Rgba32(240, 240, 240);
        }

        return default;
    }

    private static Rgba32 PixelArmor(int x, int y, int size)
    {
        if (x is >= 9 and <= 22 && y is >= 10 and <= 22 && (x + y) % 3 == 0)
        {
            return new Rgba32(70, 130, 220);
        }

        return default;
    }

    private static Rgba32 PixelAmmoBox(int x, int y, int size)
    {
        if (x is >= 8 and <= 23 && y is >= 12 and <= 24)
        {
            return new Rgba32(90, 70, 40);
        }

        if (x is >= 12 and <= 19 && y is >= 14 and <= 18)
        {
            return new Rgba32(220, 200, 60);
        }

        return default;
    }

    private static Rgba32 PixelKey(int x, int y, int size)
    {
        if (x is >= 14 and <= 22 && y is >= 10 and <= 16)
        {
            return new Rgba32(80, 140, 255);
        }

        if (x is >= 8 and <= 14 && y is >= 14 and <= 18)
        {
            return new Rgba32(120, 180, 255);
        }

        return default;
    }

    private static Rgba32 PixelExitPad(int x, int y, int size)
    {
        if (x is >= 6 and <= 25 && y is >= 14 and <= 26)
        {
            return new Rgba32(40, 200, 80, (byte)(120 + (x + y) % 80));
        }

        return default;
    }

    private static Rgba32 PixelBarrel(int x, int y, int size)
    {
        var cx = 16f;
        var cy = 18f;
        var dx = x - cx;
        var dy = y - cy;
        if (dx * dx + dy * dy > 11 * 11)
        {
            return default;
        }

        if (y % 4 < 2)
        {
            return new Rgba32(220, 60, 30);
        }

        return new Rgba32(160, 40, 20);
    }

    private static void Set(Rgba32[] atlas, int fw, int fh, int ox, int x, int y, Rgba32 color)
    {
        if (x < 0 || x >= fw || y < 0 || y >= fh)
        {
            return;
        }

        atlas[y * fw + ox + x] = color;
    }

    private static void FillRect(
        Rgba32[] atlas,
        int fw,
        int fh,
        int ox,
        int x,
        int y,
        int w,
        int h,
        Rgba32 color)
    {
        for (var py = y; py < y + h; py++)
        {
            for (var px = x; px < x + w; px++)
            {
                Set(atlas, fw, fh, ox, px, py, color);
            }
        }
    }

    private static void FillEllipse(
        Rgba32[] atlas,
        int fw,
        int fh,
        int ox,
        float cx,
        float cy,
        float rx,
        float ry,
        Rgba32 color)
    {
        var x0 = (int)(cx - rx);
        var x1 = (int)(cx + rx);
        var y0 = (int)(cy - ry);
        var y1 = (int)(cy + ry);
        for (var y = y0; y <= y1; y++)
        {
            for (var x = x0; x <= x1; x++)
            {
                var dx = (x - cx) / rx;
                var dy = (y - cy) / ry;
                if (dx * dx + dy * dy <= 1f)
                {
                    Set(atlas, fw, fh, ox, x, y, color);
                }
            }
        }
    }

    private enum DoomArchetype
    {
        Marine,
        Zombie,
        Imp,
        Pinky,
    }
}

internal enum PickupArtKind
{
    Health,
    Armor,
    Ammo,
    BlueKey,
    Exit,
    Barrel,
}
