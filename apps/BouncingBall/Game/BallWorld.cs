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
    private int _passSeed;
    private Random _passRng;
    private readonly List<Ball> _balls = [];
    private readonly BallSoA _soa = new();
    private readonly SimdGridDepenetration _ballBallSolver = new();
    private RoomInteriorBounds _bounds;
    private bool _pileSettled;

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
    public bool BallBallSkippedLastFrame { get; private set; }

    public BallWorld(BvhStaticWorld collisionWorld, int? passSeed = null)
    {
        _collisionWorld = collisionWorld;
        _passSeed = passSeed ?? unchecked(Environment.TickCount ^ (int)0x5DEECE66D);
        _passRng = new Random(_passSeed);
    }

    public void BindRoom(RoomWorld room) => _bounds = room.InteriorBounds;

    public void SpawnBall(RoomWorld room)
    {
        BindRoom(room);
        var ball = CreatePassBall(room, spreadSpawn: false);
        _balls.Add(ball);
        _pileSettled = false;
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
            _balls.Add(CreatePassBall(room, spreadSpawn: true, batchIndex: i, batchSize: count));

        _pileSettled = false;
        DepenetrateSpawnedRange(start, _balls.Count);
    }

    public void ClearAndSpawnOne(RoomWorld room)
    {
        _balls.Clear();
        _pileSettled = false;
        ChainPassSeed(_passRng.NextDouble(), _passRng.NextDouble());
        SpawnBall(room);
    }

    public void Step(float deltaSeconds)
    {
        if (deltaSeconds <= 0f || _balls.Count == 0)
            return;

        BallBallContactsLastFrame = 0;
        BallBallPairChecksLastFrame = 0;
        BallBallSkippedLastFrame = false;
        IntegratorReflectionsLastFrame = 0;
        ClampedBallsLastFrame = 0;
        ActiveBallCount = 0;
        SleepingBallCount = 0;

        var subSteps = SubStepsForCount(_balls.Count);
        var solveIterations = SolveIterationsForCount(_balls.Count);
        PhysicsSubStepsLastFrame = subSteps;

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

        if (_balls.Count > 1)
        {
            if (ActiveBallCount == 0 && _pileSettled)
            {
                BallBallSkippedLastFrame = true;
                BallBallSolveIterationsLastFrame = 0;
            }
            else
            {
                ResolveBallOverlapsSimd(ActiveBallCount, solveIterations);
            }
        }

        foreach (var ball in _balls)
        {
            if (ClampToInterior(ball))
                ClampedBallsLastFrame++;
            ClampVelocity(ball);
            UpdateSleeping(ball);
        }
    }

    private Ball CreatePassBall(RoomWorld room, bool spreadSpawn, int batchIndex = 0, int batchSize = 1)
    {
        var (spawn, velocity) = spreadSpawn
            ? SampleCircleWallPass(room, batchIndex, batchSize)
            : SamplePerimeterPass(room);
        ClampToInterior(spawn, velocity, out spawn, out velocity);
        return new Ball(spawn, velocity);
    }

    /// <summary>Mix last pass-direction rolls into the next spawn seed.</summary>
    private void ChainPassSeed(double roll0, double roll1, double roll2 = 0, int salt = 0)
    {
        _passSeed = unchecked((int)(
            (uint)_passSeed * 1597334677u
            + (uint)(roll0 * uint.MaxValue)
            + (uint)(roll1 * uint.MaxValue) * 3812015801u
            + (uint)(roll2 * uint.MaxValue) * 3965336299u
            + (uint)salt * 1442695041u));
        if (_passSeed == 0)
            _passSeed = 1;
        _passRng = new Random(_passSeed);
    }

    /// <summary>Single-player chest pass from a random wall side toward the interior.</summary>
    private (Vector3 Spawn, Vector3 Velocity) SamplePerimeterPass(RoomWorld room)
    {
        var yawRoll = _passRng.NextDouble();
        var speedRoll = _passRng.NextDouble();
        ChainPassSeed(yawRoll, speedRoll);

        var bounds = room.InteriorBounds;
        var yaw = (float)(yawRoll * MathF.Tau);
        var horizontal = new Vector3(MathF.Sin(yaw), 0f, MathF.Cos(yaw));
        var spawn = room.RoomCenter - horizontal * WallStandRadius(room, horizontal) + new Vector3(0f, 1.15f, 0f);
        spawn = ClampPosition(spawn, bounds);
        var speed = PassSpeedMin + (float)speedRoll * (PassSpeedMax - PassSpeedMin);
        var direction = Vector3.Normalize(horizontal + new Vector3(0f, PassLoft, 0f));
        return (spawn, direction * speed);
    }

    /// <summary>
    /// Bulk spawn: stand on a circle near the wall (back to wall), evenly spaced by angle,
    /// chest-pass toward <see cref="RoomWorld.RoomCenter"/>.
    /// </summary>
    private (Vector3 Spawn, Vector3 Velocity) SampleCircleWallPass(
        RoomWorld room,
        int batchIndex,
        int batchSize)
    {
        var baseYawRoll = _passRng.NextDouble();
        var yawJitterRoll = _passRng.NextDouble();
        var speedRoll = _passRng.NextDouble();
        ChainPassSeed(baseYawRoll, yawJitterRoll, speedRoll, batchIndex);

        var bounds = room.InteriorBounds;
        var baseYaw = (float)(baseYawRoll * MathF.Tau);
        var slotYaw = baseYaw + MathF.Tau * batchIndex / batchSize;
        var jitterSpan = MathF.Tau / Math.Max(batchSize, 1);
        var yaw = slotYaw + (float)((yawJitterRoll - 0.5) * jitterSpan * 0.55f);

        var towardCenter = new Vector3(MathF.Sin(yaw), 0f, MathF.Cos(yaw));
        var standRadius = WallStandRadius(room, towardCenter);
        var spawn = room.RoomCenter - towardCenter * standRadius + new Vector3(0f, 1.05f, 0f);
        spawn = ClampPosition(spawn, bounds);

        var speed = PassSpeedMin + (float)speedRoll * (PassSpeedMax - PassSpeedMin);
        var direction = Vector3.Normalize(towardCenter + new Vector3(0f, PassLoft, 0f));
        return (spawn, direction * speed);
    }

    /// <summary>Distance from room center to a wall-adjacent stand point along <paramref name="towardCenter"/>.</summary>
    private static float WallStandRadius(RoomWorld room, Vector3 towardCenter)
    {
        var bounds = room.InteriorBounds;
        var c = room.RoomCenter;
        var dx = towardCenter.X;
        var dz = towardCenter.Z;
        var len = MathF.Sqrt(dx * dx + dz * dz);
        if (len < 1e-5f)
        {
            var fallback = MathF.Min(c.X - bounds.MinX, c.Z - bounds.MinZ);
            return fallback * 0.94f;
        }

        dx /= len;
        dz /= len;

        var radius = float.MaxValue;
        if (dx > 1e-5f)
            radius = MathF.Min(radius, (c.X - bounds.MinX) / dx);
        else if (dx < -1e-5f)
            radius = MathF.Min(radius, (bounds.MaxX - c.X) / -dx);

        if (dz > 1e-5f)
            radius = MathF.Min(radius, (c.Z - bounds.MinZ) / dz);
        else if (dz < -1e-5f)
            radius = MathF.Min(radius, (bounds.MaxZ - c.Z) / -dz);

        return radius * 0.94f;
    }

    private void DepenetrateSpawnedRange(int startIndex, int endIndex)
    {
        var count = endIndex - startIndex;
        var iterations = count switch
        {
            > 50 => 12,
            > 10 => 8,
            _ => 6,
        };
        var minDist = Ball.Radius * 2.001f;
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

    private static bool SeparatePair(Ball a, Ball b, float minDist, float minDistSq, bool applyImpulse)
    {
        var delta = b.Position - a.Position;
        var distSq = delta.LengthSquared();
        if (distSq >= minDistSq)
            return false;

        Vector3 normal;
        float overlap;
        if (distSq < 1e-10f)
        {
            normal = new Vector3(1f, 0f, 0f);
            overlap = minDist;
        }
        else
        {
            var dist = MathF.Sqrt(distSq);
            normal = delta / dist;
            overlap = minDist - dist;
        }

        a.Position -= normal * (overlap * 0.5f);
        b.Position += normal * (overlap * 0.5f);

        if (!applyImpulse)
            return true;

        var relVel = b.Velocity - a.Velocity;
        var vn = Vector3.Dot(relVel, normal);
        if (vn >= 0f)
            return true;

        var impulse = -(1f + (float)BallBallRestitution) * vn * 0.5f;
        a.Velocity -= normal * impulse;
        b.Velocity += normal * impulse;
        return true;
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

    private static int DepenetrateIterationsForCount(int count) =>
        count switch
        {
            > 1000 => 4,
            > 300 => 3,
            > 80 => 2,
            _ => 1,
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

    private void ResolveBallOverlapsSimd(int activeCount, int impulseIterations)
    {
        var allSleeping = activeCount == 0;
        var positionIters = allSleeping ? 1 : DepenetrateIterationsForCount(_balls.Count);
        var impulseIters = activeCount > 1 ? impulseIterations : 0;
        BallBallSolveIterationsLastFrame = positionIters + impulseIters;

        _soa.SyncFrom(_balls);
        var frameContacts = 0;

        for (var iter = 0; iter < positionIters; iter++)
        {
            var r = _ballBallSolver.Resolve(_soa, GridCellSize, applyImpulses: false, awakePairsOnly: false);
            BallBallPairChecksLastFrame += r.PairChecks;
            frameContacts += r.Contacts;
        }

        for (var iter = 0; iter < impulseIters; iter++)
        {
            var r = _ballBallSolver.Resolve(_soa, GridCellSize, applyImpulses: true, awakePairsOnly: true);
            BallBallPairChecksLastFrame += r.PairChecks;
            frameContacts += r.Contacts;
        }

        BallBallContactsLastFrame = frameContacts;
        _soa.SyncTo(_balls);

        if (allSleeping)
            _pileSettled = frameContacts == 0;
        else
            _pileSettled = false;

        if (impulseIters > 0)
        {
            foreach (var ball in _balls)
                ClampVelocity(ball);
        }
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

}
