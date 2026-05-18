using Novolis.Physics.Collision.Simple;

namespace RagdollPlay.Game;

internal static class RagdollSelfCollision
{
    public static void Resolve(IList<SphereState> spheres, float radius, int iterations = 6)
    {
        if (spheres.Count < 2)
            return;

        for (var pass = 0; pass < iterations; pass++)
        {
            for (var i = 0; i < spheres.Count; i++)
            {
                for (var j = i + 1; j < spheres.Count; j++)
                {
                    var a = spheres[i];
                    var b = spheres[j];
                    SphereOverlapResolution.Separate(ref a.Position, ref b.Position, radius, 1.02f);
                }
            }
        }
    }
}
