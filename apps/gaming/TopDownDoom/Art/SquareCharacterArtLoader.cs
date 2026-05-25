using Novolis.Rendering.TwoD;

namespace TopDownDoom.Art;

/// <summary>
/// CC0 square characters — one zip, cohesive style.
/// https://opengameart.org/content/hand-drawn-square-characters-animated-8-directions-top-down-free-cc0
/// </summary>
internal static class SquareCharacterArtLoader
{
    public static bool TryLoad(
        TwoDTextureRegistry registry,
        string root,
        out CharacterAnimationSet player,
        out CharacterAnimationSet fodder,
        out CharacterAnimationSet imp,
        out CharacterAnimationSet bruiser,
        out TwoDAnimationClip? explosion,
        out string label)
    {
        player = null!;
        fodder = null!;
        imp = null!;
        bruiser = null!;
        explosion = null;
        label = string.Empty;

        var sprites = FindSpritesFolder(root);
        if (sprites is null)
        {
            return false;
        }

        var hero = Path.Combine(sprites, "Hero");
        var skeleton = Path.Combine(sprites, "Skeleton");
        var monster = Path.Combine(sprites, "Monster");
        var deathFx = Path.Combine(sprites, "Death FX");

        var heroWalk = CharacterAtlasBuilder.TryBuildClipFromNamePrefix(registry, hero, "idle_down", 9f);
        if (heroWalk is null)
        {
            return false;
        }

        player = new CharacterAnimationSet(heroWalk, 0.58f, heroWalk);
        fodder = LoadRole(registry, skeleton, heroWalk, 0.52f);
        imp = LoadRole(registry, monster, heroWalk, 0.5f);
        bruiser = LoadRole(registry, monster, heroWalk, 0.68f);
        explosion = CharacterAtlasBuilder.TryBuildClipFromFolder(registry, deathFx, 14f);
        label = "CC0 square characters (Hero / Skeleton / Monster)";
        return true;
    }

    private static CharacterAnimationSet LoadRole(
        TwoDTextureRegistry registry,
        string folder,
        TwoDAnimationClip fallback,
        float halfHeight)
    {
        var clip = CharacterAtlasBuilder.TryBuildClipFromNamePrefix(registry, folder, "idle_down", 9f) ?? fallback;
        return new CharacterAnimationSet(clip, halfHeight, clip);
    }

    private static string? FindSpritesFolder(string root)
    {
        if (!Directory.Exists(root))
        {
            return null;
        }

        var direct = Path.Combine(root, "Sprites");
        if (Directory.Exists(direct))
        {
            return direct;
        }

        foreach (var dir in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories))
        {
            if (Path.GetFileName(dir).Equals("Sprites", StringComparison.OrdinalIgnoreCase))
            {
                return dir;
            }
        }

        return null;
    }
}
