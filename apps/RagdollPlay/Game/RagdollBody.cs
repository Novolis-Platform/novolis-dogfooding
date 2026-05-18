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
            LinearDragPerSecond = 0.18,
            SphereRestitution = 0.25f,
            StaticRestitution = 0.35f,
            MaxSpeedMps = 12f,
        },
        JointIterations = 18,
        JointRelaxIterations = 4,
        InternalCollisionIterations = 6,
        ConstraintPasses = 2,
    };

    private readonly List<SphereState> _spheres = [];
    private readonly List<DistanceJoint> _joints = [];
    private readonly List<SwingLimit> _swingLimits = [];
    private readonly List<HingeLimit> _hingeLimits = [];
    private InteriorClampVolume _clamp;

    public IReadOnlyList<SphereState> Spheres => _spheres;
    public IReadOnlyList<DistanceJoint> Joints => _joints;
    public int LastJointCorrections => _simulator.LastJointCorrections;
    public int LastInternalFixes => _simulator.LastInternalCollisionFixes;

    public void SpawnStanding(Vector3 groundPoint, PlayRoom room)
    {
        _spheres.Clear();
        _joints.Clear();
        _swingLimits.Clear();
        _hingeLimits.Clear();
        _clamp = room.InteriorBounds.ToInteriorClamp();

        var hip = groundPoint + new Vector3(0f, 1.02f, 0f);
        var chest = hip + new Vector3(0f, 0.5f, 0.02f);
        var head = chest + new Vector3(0f, 0.4f, 0f);
        var lKnee = hip + new Vector3(-0.2f, -0.5f, 0.06f);
        var rKnee = hip + new Vector3(0.2f, -0.5f, 0.06f);
        var lFoot = lKnee + new Vector3(0f, -0.42f, 0.1f);
        var rFoot = rKnee + new Vector3(0f, -0.42f, 0.1f);
        var lShoulder = chest + new Vector3(-0.3f, 0.1f, 0.1f);
        var rShoulder = chest + new Vector3(0.3f, 0.1f, 0.1f);
        var lHand = lShoulder + new Vector3(-0.26f, -0.06f, 0.16f);
        var rHand = rShoulder + new Vector3(0.26f, -0.06f, 0.16f);

        AddSphere(hip);
        AddSphere(lKnee);
        AddSphere(rKnee);
        AddSphere(chest);
        AddSphere(head);
        AddSphere(lShoulder);
        AddSphere(rShoulder);
        AddSphere(lHand);
        AddSphere(rHand);
        AddSphere(lFoot);
        AddSphere(rFoot);

        Link(RagdollIndices.Hip, RagdollIndices.Chest);
        Link(RagdollIndices.Chest, RagdollIndices.Head);
        Link(RagdollIndices.Hip, RagdollIndices.LeftKnee);
        Link(RagdollIndices.Hip, RagdollIndices.RightKnee);
        Link(RagdollIndices.LeftKnee, RagdollIndices.LeftFoot);
        Link(RagdollIndices.RightKnee, RagdollIndices.RightFoot);
        Link(RagdollIndices.Chest, RagdollIndices.LeftShoulder);
        Link(RagdollIndices.Chest, RagdollIndices.RightShoulder);
        Link(RagdollIndices.LeftShoulder, RagdollIndices.LeftHand);
        Link(RagdollIndices.RightShoulder, RagdollIndices.RightHand);

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
        _simulator.SetJoints(CollectionsMarshal.AsSpan(_joints));
        _simulator.Step(world, _spheres, _clamp, deltaSeconds);
    }

    private void StabilizeSpawn()
    {
        var joints = CollectionsMarshal.AsSpan(_joints);
        var swings = CollectionsMarshal.AsSpan(_swingLimits);
        var hinges = CollectionsMarshal.AsSpan(_hingeLimits);

        for (var i = 0; i < 32; i++)
        {
            DistanceJointSolver.Solve(joints, _spheres, 10);
            AngularLimitSolver.Solve(swings, hinges, _spheres, 2);
        }

        _simulator.DepenetrateSpawnedRange(_spheres, 0, _spheres.Count, _clamp);

        foreach (var sphere in _spheres)
        {
            sphere.Velocity = Vector3.Zero;
            sphere.IsSleeping = false;
        }

        _simulator.ResetPileState();
    }

    private void WakeAll()
    {
        foreach (var s in _spheres)
            s.IsSleeping = false;
        _simulator.MarkPileUnsettled();
    }

    private void AddSphere(Vector3 position) =>
        _spheres.Add(new SphereState(position, Vector3.Zero));

    private void Link(int a, int b) =>
        _joints.Add(new DistanceJoint(a, b, Vector3.Distance(_spheres[a].Position, _spheres[b].Position), 1f));
}
