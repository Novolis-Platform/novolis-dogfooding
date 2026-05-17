using System.Numerics;
using Novolis.Physics.Collision.Simple;

namespace BouncingBall.Game;

/// <summary>Many balls in one room: world collision, spatial ball-ball, quick passes.</summary>
internal sealed class BallWorld
{
    private static readonly Vector3 Gravity = new(0f, -9.80665f, 0f);

    private const double LinearDragPerSecond = 0.048;
    private const double WallRestitution = 0.82;
    private const double BallBallRestitution = 0.88;
    private const double GroundFrictionPerSecond = 9.5;
    private const float FloorY = 0f;
    private const float GroundContactSlack = 0.05f;
    private const float SleepSpeedThreshold = 0.12f;
    private const float MaxSpeedMps = 18f;
    private const float PassSpeedMin = 7.2f;
    private const float PassSpeedMax = 9.2f;
    private const float PassLoft = 0.22f;
    private const float GridCellSize = Ball.Radius * 2.25f;

    private readonly BvhStaticWorld _collisionWorld;
    private readonly Random _rng;
    private readonly List<Ball> _balls = [];
    private readonly Dictionary<int, List<int>> _spatialGrid = new();
    private readonly Stack<List<int>> _cellListPool = new();
    private RoomInteriorBounds _bounds;

    public IReadOnlyList<Ball> Balls => _balls;
    public int BallCount => _balls.Count;
    public int ActiveBallCount { get; private set; }
    public int SleepingBallCount { get; private set; }
    public int BallBallContactsLastFrame { get; private set; }
    public int BallBallPairChecksLastFrame { get; private set; }
    public int IntegratorReflectionsLastFrame { get; private set; }
    public int PhysicsSubStepsLastFrame { get; private set; }
    public int BallBallSolveIterationsLastFrame { get; private set; }
    public int ClampedBallsLastFrame { get; private set; }

    public BallWorld(BvhStaticWorld collisionWorld, Random? rng = null)
    {
        _collisionWorld = collisionWorld;
        _rng = rng ?? Random.Shared;
    }

    public void BindRoom(RoomWorld room) => _bounds = room.InteriorBounds;

    public void SpawnBall(RoomWorld room)
    {
        BindRoom(room);
        var ball = CreatePassBall(room, spreadSpawn: false);
        _balls.Add(ball);
        DepenetrateSpawnedRange(_balls.Count - 1, _balls.Count);
    }

    public void SpawnBalls(RoomWorld room, int count)
    {
        if (count <= 0)
            return;

        BindRoom(room);
        var start = _balls.Count;
        var target = start + count;
        if (_balls.Capacity < target)
            _balls.Capacity = target;

        for (var i = 0; i < count; i++)
            _balls.Add(CreatePassBall(room, spreadSpawn: true));

        DepenetrateSpawnedRange(start, _balls.Count);
    }

    public void ClearAndSpawnOne(RoomWorld room)
    {
        _balls.Clear();
        SpawnBall(room);
    }

    public void Step(float deltaSeconds)
    {
        if (deltaSeconds <= 0f || _balls.Count == 0)
            return;

        BallBallContactsLastFrame = 0;
        BallBallPairChecksLastFrame = 0;
        IntegratorReflectionsLastFrame = 0;
        ClampedBallsLastFrame = 0;
        ActiveBallCount = 0;
        SleepingBallCount = 0;

        var subSteps = SubStepsForCount(_balls.Count);
        var solveIterations = SolveIterationsForCount(_balls.Count);
        PhysicsSubStepsLastFrame = subSteps;
        BallBallSolveIterationsLastFrame = solveIterations;

        foreach (var ball in _balls)
        {
            UpdateSleeping(ball);
            if (ball.IsSleeping)
            {
                SleepingBallCount++;
                continue;
            }

            ActiveBallCount++;
            var pos = ball.Position;
            var vel = ball.Velocity;
            IntegratorReflectionsLastFrame += BvhStaticSphereIntegrator.AdvanceWithUniformAccelerationAndLinearDrag(
                _collisionWorld,
                ref pos,
                ref vel,
                Ball.Radius,
                deltaSeconds,
                Gravity,
                LinearDragPerSecond,
                substepsPerStep: subSteps,
                normalRestitution: WallRestitution);
            ball.Position = pos;
            ball.Velocity = vel;
            ApplyGroundFriction(ball, deltaSeconds);
            UpdateSleeping(ball);
            if (ball.IsSleeping)
            {
                SleepingBallCount++;
                ActiveBallCount--;
            }
        }

        if (ActiveBallCount > 1)
        {
            for (var iter = 0; iter < solveIterations; iter++)
                ResolveBallBallContactsSpatial();
        }

        foreach (var ball in _balls)
        {
            if (ClampToInterior(ball))
                ClampedBallsLastFrame++;
            ClampVelocity(ball);
            UpdateSleeping(ball);
        }
    }

    private Ball CreatePassBall(RoomWorld room, bool spreadSpawn)
    {
        var (spawn, velocity) = spreadSpawn
            ? SampleInteriorPass(room, _rng)
            : SamplePerimeterPass(room, _rng);
        ClampToInterior(spawn, velocity, out spawn, out velocity);
        return new Ball(spawn, velocity);
    }

    /// <summary>Single-player chest pass from a random wall side toward the interior.</summary>
    private static (Vector3 Spawn, Vector3 Velocity) SamplePerimeterPass(RoomWorld room, Random rng)
    {
        var bounds = room.InteriorBounds;
        var yaw = (float)(rng.NextDouble() * MathF.Tau);
        var horizontal = new Vector3(MathF.Sin(yaw), 0f, MathF.Cos(yaw));
        var passDistance = (RoomWorld.GridSize - 2) * RoomWorld.CellSize * 0.4f;
        var spawn = room.RoomCenter - horizontal * passDistance + new Vector3(0f, 1.15f, 0f);
        spawn = ClampPosition(spawn, bounds);
        var speed = PassSpeedMin + (float)rng.NextDouble() * (PassSpeedMax - PassSpeedMin);
        var direction = Vector3.Normalize(horizontal + new Vector3(0f, PassLoft, 0f));
        return (spawn, direction * speed);
    }

    /// <summary>Random point in the court with a pass toward a random interior target (bulk spawn).</summary>
    private static (Vector3 Spawn, Vector3 Velocity) SampleInteriorPass(RoomWorld room, Random rng)
    {
        var bounds = room.InteriorBounds;
        var spawn = new Vector3(
            Lerp(bounds.MinX, bounds.MaxX, (float)rng.NextDouble()),
            Lerp(0.9f, 2.1f, (float)rng.NextDouble()),
            Lerp(bounds.MinZ, bounds.MaxZ, (float)rng.NextDouble()));

        var target = new Vector3(
            Lerp(bounds.MinX, bounds.MaxX, (float)rng.NextDouble()),
            spawn.Y,
            Lerp(bounds.MinZ, bounds.MaxZ, (float)rng.NextDouble()));

        var toTarget = target - spawn;
        toTarget.Y = 0f;
        if (toTarget.LengthSquared() < 1e-4f)
            toTarget = new Vector3(1f, 0f, 0f);

        var direction = Vector3.Normalize(toTarget + new Vector3(0f, PassLoft, 0f));
        var speed = PassSpeedMin + (float)rng.NextDouble() * (PassSpeedMax - PassSpeedMin);
        return (spawn, direction * speed);
    }

    private void DepenetrateSpawnedRange(int startIndex, int endIndex)
    {
        const int iterations = 6;
        var minDist = Ball.Radius * 2.05f;
        var minDistSq = minDist * minDist;

        for (var iter = 0; iter < iterations; iter++)
        {
            for (var i = startIndex; i < endIndex; i++)
            {
                for (var j = i + 1; j < endIndex; j++)
                    SeparatePair(_balls[i], _balls[j], minDist, minDistSq, applyImpulse: false);

                for (var j = 0; j < startIndex; j++)
                    SeparatePair(_balls[i], _balls[j], minDist, minDistSq, applyImpulse: false);
            }
        }

        for (var i = startIndex; i < endIndex; i++)
        {
            ClampToInterior(_balls[i]);
            ClampVelocity(_balls[i]);
        }
    }

    private static void SeparatePair(Ball a, Ball b, float minDist, float minDistSq, bool applyImpulse)
    {
        var delta = b.Position - a.Position;
        var distSq = delta.LengthSquared();
        if (distSq >= minDistSq || distSq < 1e-12f)
            return;

        var dist = MathF.Sqrt(distSq);
        var normal = delta / dist;
        var overlap = minDist - dist;
        a.Position -= normal * (overlap * 0.5f);
        b.Position += normal * (overlap * 0.5f);

        if (!applyImpulse)
            return;

        var relVel = b.Velocity - a.Velocity;
        var vn = Vector3.Dot(relVel, normal);
        if (vn >= 0f)
            return;

        var impulse = -(1f + (float)BallBallRestitution) * vn * 0.5f;
        a.Velocity -= normal * impulse;
        b.Velocity += normal * impulse;
    }

    private static void UpdateSleeping(Ball ball) =>
        ball.IsSleeping = ball.IsGrounded && ball.Speed < SleepSpeedThreshold;

    private static int SubStepsForCount(int count) =>
        count switch
        {
            > 200 => 4,
            > 80 => 8,
            > 30 => 12,
            _ => 16,
        };

    private static int SolveIterationsForCount(int count) =>
        count switch
        {
            > 150 => 1,
            > 50 => 2,
            _ => 3,
        };

    private void ApplyGroundFriction(Ball ball, float deltaSeconds)
    {
        var floorContactY = FloorY + Ball.Radius;
        ball.IsGrounded = ball.Position.Y <= floorContactY + GroundContactSlack && ball.Velocity.Y <= 1.2f;
        if (!ball.IsGrounded)
            return;

        if (ball.Position.Y < floorContactY)
            ball.Position = new Vector3(ball.Position.X, floorContactY, ball.Position.Z);

        var horizontalSpeed = MathF.Sqrt(ball.Velocity.X * ball.Velocity.X + ball.Velocity.Z * ball.Velocity.Z);
        if (horizontalSpeed > 1e-5f)
        {
            var scale = MathF.Max(0f, 1f - (float)(GroundFrictionPerSecond * deltaSeconds));
            ball.Velocity = new Vector3(ball.Velocity.X * scale, ball.Velocity.Y, ball.Velocity.Z * scale);
        }

        if (MathF.Abs(ball.Velocity.Y) < 0.6f)
            ball.Velocity = new Vector3(ball.Velocity.X, MathF.Min(ball.Velocity.Y, 0f), ball.Velocity.Z);
    }

    private void ResolveBallBallContactsSpatial()
    {
        ClearSpatialGrid();
        for (var i = 0; i < _balls.Count; i++)
            AddToSpatialGrid(i, _balls[i].Position);

        for (var i = 0; i < _balls.Count; i++)
        {
            var a = _balls[i];
            if (a.IsSleeping)
                continue;

            var cx = (int)MathF.Floor(a.Position.X / GridCellSize);
            var cz = (int)MathF.Floor(a.Position.Z / GridCellSize);

            for (var dx = -1; dx <= 1; dx++)
            for (var dz = -1; dz <= 1; dz++)
            {
                var key = CellKey(cx + dx, cz + dz);
                if (!_spatialGrid.TryGetValue(key, out var cell))
                    continue;

                foreach (var j in cell)
                {
                    if (j <= i)
                        continue;

                    BallBallPairChecksLastFrame++;
                    TryResolvePair(a, _balls[j]);
                }
            }
        }
    }

    private void TryResolvePair(Ball a, Ball b)
    {
        if (a.IsSleeping && b.IsSleeping)
            return;

        var minDist = Ball.Radius * 2f;
        var minDistSq = minDist * minDist;
        var delta = b.Position - a.Position;
        if (delta.LengthSquared() >= minDistSq)
            return;

        BallBallContactsLastFrame++;
        SeparatePair(a, b, minDist, minDistSq, applyImpulse: !(a.IsSleeping || b.IsSleeping));
        ClampVelocity(a);
        ClampVelocity(b);
    }

    private bool ClampToInterior(Ball ball)
    {
        var before = ball.Position;
        ball.Position = ClampPosition(ball.Position, _bounds);
        ball.Velocity = ClampVelocity(ball.Velocity);
        return (ball.Position - before).LengthSquared() > 1e-8f;
    }

    private static void ClampToInterior(
        Vector3 spawn,
        Vector3 velocity,
        out Vector3 clampedSpawn,
        out Vector3 clampedVelocity)
    {
        clampedSpawn = spawn;
        clampedVelocity = ClampVelocity(velocity);
    }

    private static Vector3 ClampPosition(Vector3 p, RoomInteriorBounds b) =>
        new(
            Math.Clamp(p.X, b.MinX, b.MaxX),
            Math.Clamp(p.Y, b.MinY, b.MaxY),
            Math.Clamp(p.Z, b.MinZ, b.MaxZ));

    private static void ClampVelocity(Ball ball) => ball.Velocity = ClampVelocity(ball.Velocity);

    private static Vector3 ClampVelocity(Vector3 v)
    {
        var speed = v.Length();
        if (speed <= MaxSpeedMps || speed < 1e-6f)
            return v;

        return v * (MaxSpeedMps / speed);
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    private void ClearSpatialGrid()
    {
        foreach (var list in _spatialGrid.Values)
        {
            list.Clear();
            _cellListPool.Push(list);
        }

        _spatialGrid.Clear();
    }

    private void AddToSpatialGrid(int ballIndex, Vector3 position)
    {
        var key = CellKeyFromPosition(position);
        if (!_spatialGrid.TryGetValue(key, out var list))
        {
            list = _cellListPool.Count > 0 ? _cellListPool.Pop() : new List<int>(8);
            _spatialGrid[key] = list;
        }

        list.Add(ballIndex);
    }

    private static int CellKeyFromPosition(Vector3 position) =>
        CellKey(
            (int)MathF.Floor(position.X / GridCellSize),
            (int)MathF.Floor(position.Z / GridCellSize));

    private static int CellKey(int cellX, int cellZ) => cellX * 4096 + cellZ;
}
