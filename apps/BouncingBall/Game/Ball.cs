using System.Numerics;

namespace BouncingBall.Game;

internal sealed class Ball
{
    public const float Radius = 0.22f;

    public Vector3 Position;
    public Vector3 Velocity;
    public bool IsGrounded;
    public bool IsSleeping;

    public float Speed => Velocity.Length();

    public Ball(Vector3 position, Vector3 velocity)
    {
        Position = position;
        Velocity = velocity;
    }
}
