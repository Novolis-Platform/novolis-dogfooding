using System.Numerics;

namespace BouncingBall.Game;

/// <summary>Structure-of-arrays ball state for cache-friendly SIMD broad-phase.</summary>
internal sealed class BallSoA
{
    public float[] PosX = [];
    public float[] PosY = [];
    public float[] PosZ = [];
    public float[] VelX = [];
    public float[] VelY = [];
    public float[] VelZ = [];
    public bool[] Sleeping = [];

    public int Count { get; private set; }

    public void Resize(int count)
    {
        Count = count;
        if (PosX.Length < count)
        {
            PosX = new float[count];
            PosY = new float[count];
            PosZ = new float[count];
            VelX = new float[count];
            VelY = new float[count];
            VelZ = new float[count];
            Sleeping = new bool[count];
        }
    }

    public void SyncFrom(IReadOnlyList<Ball> balls)
    {
        Resize(balls.Count);
        for (var i = 0; i < balls.Count; i++)
        {
            var b = balls[i];
            PosX[i] = b.Position.X;
            PosY[i] = b.Position.Y;
            PosZ[i] = b.Position.Z;
            VelX[i] = b.Velocity.X;
            VelY[i] = b.Velocity.Y;
            VelZ[i] = b.Velocity.Z;
            Sleeping[i] = b.IsSleeping;
        }
    }

    public void SyncTo(IList<Ball> balls)
    {
        for (var i = 0; i < Count; i++)
        {
            var b = balls[i];
            b.Position = new Vector3(PosX[i], PosY[i], PosZ[i]);
            b.Velocity = new Vector3(VelX[i], VelY[i], VelZ[i]);
        }
    }
}
