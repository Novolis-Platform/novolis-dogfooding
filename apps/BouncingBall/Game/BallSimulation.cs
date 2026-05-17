using System.Numerics;
using Novolis.Physics.Collision.Simple;

namespace BouncingBall.Game;

internal sealed class BallSimulation
{
    public const float Radius = 0.22f;

    /// <summary>SI standard gravity (m/s²), +Y up.</summary>
    private static readonly Vector3 Gravity = new(0f, -9.80665f, 0f);

    private const int SubStepsPerFrame = 16;
    private const double LinearDragPerSecond = 0.048;
    private const double Restitution = 0.82;

    private static readonly Vector3 DefaultThrowDirection =
        Vector3.Normalize(new Vector3(2.35f, 4.05f, -2.65f));

    private const float DefaultThrowSpeed = 6.5f;

    private readonly BvhStaticWorld _world;
    private Vector3 _position;
    private Vector3 _velocity;

    public Vector3 Position => _position;
    public Vector3 Velocity => _velocity;
    public float Speed => _velocity.Length();

    public BallSimulation(BvhStaticWorld world, Vector3 initialPosition, Vector3 initialVelocity)
    {
        _world = world;
        _position = initialPosition;
        _velocity = initialVelocity;
    }

    public static BallSimulation CreateDefault(RoomWorld room)
    {
        var spawn = room.RoomCenter + new Vector3(-0.8f, 1.2f, -0.6f);
        var velocity = DefaultThrowDirection * DefaultThrowSpeed;
        return new BallSimulation(room.CollisionWorld, spawn, velocity);
    }

    public void Reset(RoomWorld room)
    {
        _position = room.RoomCenter + new Vector3(-0.8f, 1.2f, -0.6f);
        _velocity = DefaultThrowDirection * DefaultThrowSpeed;
    }

    public void Step(float deltaSeconds)
    {
        if (deltaSeconds <= 0f)
            return;

        BvhStaticSphereIntegrator.AdvanceWithUniformAccelerationAndLinearDrag(
            _world,
            ref _position,
            ref _velocity,
            Radius,
            deltaSeconds,
            Gravity,
            LinearDragPerSecond,
            substepsPerStep: SubStepsPerFrame,
            normalRestitution: Restitution);
    }
}
