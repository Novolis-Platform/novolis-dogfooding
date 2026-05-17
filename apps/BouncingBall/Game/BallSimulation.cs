using System.Numerics;
using Novolis.Physics.Collision.Simple;
using Novolis.Physics.Numerics;

namespace BouncingBall.Game;

internal sealed class BallSimulation
{
    public const double Radius = 0.22;
    private const int SubStepsPerFrame = 4;
    private const double Restitution = 0.95;

    private readonly BvhStaticWorld _world;
    private Vector3d _position;
    private Vector3d _velocity;

    public Vector3 Position => new((float)_position.X, (float)_position.Y, (float)_position.Z);
    public Vector3 Velocity => new((float)_velocity.X, (float)_velocity.Y, (float)_velocity.Z);
    public float Speed => (float)_velocity.Length();

    public BallSimulation(BvhStaticWorld world, Vector3d initialPosition, Vector3d initialVelocity)
    {
        _world = world;
        _position = initialPosition;
        _velocity = initialVelocity;
    }

    public static BallSimulation CreateDefault(RoomWorld room) =>
        new(
            room.CollisionWorld,
            room.RoomCenter,
            new Vector3d(2.5, -2.0, 1.8));

    public void Reset(RoomWorld room)
    {
        _position = room.RoomCenter;
        _velocity = new Vector3d(2.5, -2.0, 1.8);
    }

    public void Step(float deltaSeconds)
    {
        if (deltaSeconds <= 0f)
            return;

        var subDt = deltaSeconds / SubStepsPerFrame;
        for (var i = 0; i < SubStepsPerFrame; i++)
        {
            BvhStaticSphereIntegrator.AdvanceOneStep(
                _world,
                ref _position,
                ref _velocity,
                Radius,
                subDt,
                normalRestitution: Restitution);
        }
    }
}
