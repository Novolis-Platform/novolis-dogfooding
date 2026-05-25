using Novolis.Rendering.TwoD;

namespace TopDownDoom.Art;

internal sealed class CharacterAnimationSet(
    TwoDAnimationClip walk,
    float worldHalfHeight,
    TwoDAnimationClip? shoot = null,
    TwoDAnimationClip? death = null)
{
    public TwoDAnimationClip Walk { get; } = walk;
    public TwoDAnimationClip? Shoot { get; } = shoot;
    public TwoDAnimationClip? Death { get; } = death;
    public float WorldHalfHeight { get; } = worldHalfHeight;
}
