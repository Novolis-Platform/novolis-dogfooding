using System.Numerics;
using System.Runtime.InteropServices;
using Novolis.Physics.Collision.Simple;
using Novolis.Physics.Joints;
using Novolis.Simulation.World.Builders;

namespace RagdollPlay.Game;

internal sealed class RagdollBody
{
    public const float SphereRadius = 0.2f;

    private readonly ConstrainedSphereSimulator _simulator = new()
    {
        Options =
        {
            Radius = SphereRadius,
            LinearDragPerSecond = 0.12,
            SphereRestitution = 0.35f,
            StaticRestitution = 0.45f,
        },
        JointIterations = 14,
    };

    private readonly List<SphereState> _spheres = [];
    private readonly List<DistanceJoint> _joints = [];
    private readonly List<SwingLimit> _swingLimits = [];
    private readonly List<HingeLimit> _hingeLimits = [];
    private InteriorClampVolume _clamp;
    private int _lastInternalFixes;
    private int _lastAngularFixes;

    public IReadOnlyList<SphereState> Spheres => _spheres;
    public IReadOnlyList<DistanceJoint> Joints => _joints;
    public int LastJointCorrections => _simulator.LastJointCorrections;
    public int LastInternalFixes => _lastInternalFixes;
    public int LastAngularFixes => _lastAngularFixes;

    public void SpawnStanding(Vector3 groundPoint, PlayRoom room)
    {
        _spheres.Clear();
        _joints.Clear();
        _swingLimits.Clear();
        _hingeLimits.Clear();
        _clamp = room.InteriorBounds.ToInteriorClamp();

        var hip = groundPoint + new Vector3(0f, 1.05f, 0f);
        var chest = hip + new Vector3(0f, 0.52f, 0.02f);
        var head = chest + new Vector3(0f, 0.42f, 0f);
        var lKnee = hip + new Vector3(-0.22f, -0.48f, 0.08f);
        var rKnee = hip + new Vector3(0.22f, -0.48f, 0.08f);
        var lShoulder = chest + new Vector3(-0.32f, 0.12f, 0.12f);
        var rShoulder = chest + new Vector3(0.32f, 0.12f, 0.12f);
        var lHand = lShoulder + new Vector3(-0.28f, -0.08f, 0.18f);
        var rHand = rShoulder + new Vector3(0.28f, -0.08f, 0.18f);

        AddSphere(hip);
        AddSphere(lKnee);
        AddSphere(rKnee);
        AddSphere(chest);
        AddSphere(head);
        AddSphere(lShoulder);
        AddSphere(rShoulder);
        AddSphere(lHand);
        AddSphere(rHand);

        Link(RagdollIndices.Hip, RagdollIndices.LeftKnee, stiffness: 0.94f);
        Link(RagdollIndices.Hip, RagdollIndices.RightKnee, stiffness: 0.94f);
        Link(RagdollIndices.Hip, RagdollIndices.Chest, stiffness: 0.98f);
        Link(RagdollIndices.Chest, RagdollIndices.Head, stiffness: 0.9f);
        Link(RagdollIndices.Chest, RagdollIndices.LeftShoulder, stiffness: 0.72f);
        Link(RagdollIndices.Chest, RagdollIndices.RightShoulder, stiffness: 0.72f);
        Link(RagdollIndices.LeftShoulder, RagdollIndices.LeftHand, stiffness: 0.8f);
        Link(RagdollIndices.RightShoulder, RagdollIndices.RightHand, stiffness: 0.8f);

        RagdollPoseLimits.BuildFromSpawnPose(_spheres, _swingLimits, _hingeLimits);

        _simulator.SetJoints(CollectionsMarshal.AsSpan(_joints));
        _simulator.DepenetrateSpawnedRange(_spheres, 0, _spheres.Count, _clamp);
        StabilizeSpawn();
    }

    public void ApplyImpulse(int sphereIndex, Vector3 impulse)
    {
        if ((uint)sphereIndex >= (uint)_spheres.Count)
            return;

        _spheres[sphereIndex].Velocity += impulse;
        _spheres[sphereIndex].IsSleeping = false;
        WakeAll();
    }

    public void Step(BvhStaticWorld world, float deltaSeconds)
    {
        var joints = CollectionsMarshal.AsSpan(_joints);
        var swings = CollectionsMarshal.AsSpan(_swingLimits);
        var hinges = CollectionsMarshal.AsSpan(_hingeLimits);

        for (var pass = 0; pass < 3; pass++)
        {
            DistanceJointSolver.Solve(joints, _spheres, 10);
            _lastAngularFixes = AngularLimitSolver.Solve(swings, hinges, _spheres, 6);
            RagdollSelfCollision.Resolve(_spheres, SphereRadius, 5);
        }

        _simulator.Step(world, _spheres, _clamp, deltaSeconds);

        for (var pass = 0; pass < 3; pass++)
        {
            DistanceJointSolver.Solve(joints, _spheres, 10);
            _lastAngularFixes = AngularLimitSolver.Solve(swings, hinges, _spheres, 6);
            RagdollSelfCollision.Resolve(_spheres, SphereRadius, 5);
        }

        _lastInternalFixes = 0;
    }

    private void StabilizeSpawn()
    {
        var joints = CollectionsMarshal.AsSpan(_joints);
        var swings = CollectionsMarshal.AsSpan(_swingLimits);
        var hinges = CollectionsMarshal.AsSpan(_hingeLimits);
        for (var i = 0; i < 24; i++)
        {
            DistanceJointSolver.Solve(joints, _spheres, 12);
            AngularLimitSolver.Solve(swings, hinges, _spheres, 8);
            RagdollSelfCollision.Resolve(_spheres, SphereRadius, 8);
        }
    }

    private void WakeAll()
    {
        foreach (var s in _spheres)
            s.IsSleeping = false;
        _simulator.MarkPileUnsettled();
    }

    private void AddSphere(Vector3 position) =>
        _spheres.Add(new SphereState(position, Vector3.Zero));

    private void Link(int a, int b, float stiffness)
    {
        var rest = Vector3.Distance(_spheres[a].Position, _spheres[b].Position);
        _joints.Add(new DistanceJoint(a, b, rest, stiffness));
    }
}
