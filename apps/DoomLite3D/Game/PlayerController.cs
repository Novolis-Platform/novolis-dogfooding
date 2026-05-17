using System.Numerics;
using Novolis.Math.Geometry;
using Novolis.Physics.Abstractions;
using Novolis.Physics.Collision.Simple;
using Novolis.Raylib.Game;
using Novolis.Raylib.Interact;
using RayCamera = Novolis.Raylib.Rendering.Camera;

namespace DoomLite3D.Game;

internal sealed class PlayerController
{
    private const float MoveSpeed = 4.5f;
    private const float EyeHeight = 1.6f;
    private const float MouseSensitivity = 0.0022f;
    private const float PlayerRadius = 0.25f;

    public readonly FirstPersonCamera Camera = new();
    private IStaticWorld? _physicsWorld;

    public void SetPhysicsWorld(IStaticWorld world) => _physicsWorld = world;

    public void Reset(LevelMap level)
    {
        var spawn = level.CellToWorld(level.PlayerSpawn.X, level.PlayerSpawn.Y);
        Camera.Position = new Vector3(spawn.X, 0f, spawn.Z);
        Camera.Yaw = 0f;
        Camera.Pitch = 0f;
    }

    public void Update(RayGameContext ctx, LevelMap level, bool allowMovement)
    {
        var delta = ctx.MouseDelta;
        Camera.AddLookDelta(-delta.X * MouseSensitivity, -delta.Y * MouseSensitivity);

        if (!allowMovement)
            return;

        var move = Vector2.Zero;
        if (ctx.IsKeyDown(KeyboardKey.W))
            move += new Vector2(Camera.GetForwardXZ().X, Camera.GetForwardXZ().Z);
        if (ctx.IsKeyDown(KeyboardKey.S))
            move -= new Vector2(Camera.GetForwardXZ().X, Camera.GetForwardXZ().Z);
        if (ctx.IsKeyDown(KeyboardKey.A))
            move -= new Vector2(Camera.GetRightXZ().X, Camera.GetRightXZ().Z);
        if (ctx.IsKeyDown(KeyboardKey.D))
            move += new Vector2(Camera.GetRightXZ().X, Camera.GetRightXZ().Z);

        if (move.LengthSquared() > 1e-6f)
        {
            move = Vector2.Normalize(move) * (MoveSpeed * ctx.DeltaSeconds);
            var pos = new Vector2(Camera.Position.X, Camera.Position.Z);
            pos = _physicsWorld is null
                ? GridCollision2D.TryMove(level.Walls, pos, move, PlayerRadius, LevelMap.CellSize)
                : GridPhysicsMovement.TryMove(_physicsWorld, pos, move, PlayerRadius);
            Camera.Position = new Vector3(pos.X, 0f, pos.Y);
        }
    }

    public RayCamera BuildRaylibCamera() =>
        RayCamera.Perspective(
            Camera.GetEyePosition(EyeHeight),
            Camera.GetLookTarget(EyeHeight),
            Vector3.UnitY,
            70f);

    public Ray3 GetLookRay()
    {
        var origin = Camera.GetEyePosition(EyeHeight);
        var dir = Camera.GetLookDirection();
        return new Ray3(origin, dir);
    }
}

internal readonly struct Ray3(Vector3 origin, Vector3 direction)
{
    public Vector3 Origin { get; } = origin;
    public Vector3 Direction { get; } = direction;
}
