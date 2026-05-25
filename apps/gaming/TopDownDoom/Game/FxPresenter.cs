using Novolis.Math.Geometry;
using Novolis.Rendering.TwoD;
using TopDownDoom.Art;

namespace TopDownDoom.Game;

internal sealed class FxPresenter(CharacterArtLibrary art)
{
    public void Sync(TwoDScene scene, CombatJuice juice)
    {
        scene.AnimatedSprites.RemoveAll(s => s.SortKey is >= 200 and < 220);
        scene.StaticPolygons.RemoveAll(p => p.SortKey is >= 200 and < 220);

        foreach (var fx in juice.Effects)
        {
            switch (fx.Kind)
            {
                case CombatFxKind.Explosion when art.Explosion is not null:
                    AddBurst(scene, fx, art.Explosion, 210, fx.Scale * 1.4f);
                    break;
                case CombatFxKind.MuzzleFlash:
                    DrawFlash(scene, fx.Position, 0.35f * fx.Scale, new Rgba32(255, 240, 140, 200), 205);
                    break;
                case CombatFxKind.HitSpark:
                    DrawFlash(scene, fx.Position, 0.25f * fx.Scale, new Rgba32(255, 200, 80, 220), 206);
                    break;
                case CombatFxKind.BloodSplat:
                    DrawFlash(scene, fx.Position, 0.4f * fx.Scale, new Rgba32(180, 20, 20, 180), 207);
                    break;
            }
        }
    }

    private static void AddBurst(
        TwoDScene scene,
        CombatFx fx,
        TwoDAnimationClip clip,
        int sortKey,
        float halfHeight)
    {
        var t = fx.Time / fx.Duration;
        var alpha = 1f - t;
        var anim = new TwoDAnimatedSprite
        {
            Clip = clip,
            Loop = false,
            Time = fx.Time,
            SortKey = sortKey,
        };
        anim.Transform.Position = fx.Position;
        anim.Transform.Scale = new System.Numerics.Vector3(halfHeight, 1f, halfHeight);
        scene.AnimatedSprites.Add(anim);
    }

    private static void DrawFlash(TwoDScene scene, System.Numerics.Vector3 pos, float radius, Rgba32 color, int sort)
    {
        var poly = TwoDScenePrimitives.Rectangle(pos.X - radius, pos.Z - radius, pos.X + radius, pos.Z + radius);
        scene.StaticPolygons.Add(new TwoDStaticPolygon(poly, color) { DrawFilled = true, SortKey = sort });
    }
}
