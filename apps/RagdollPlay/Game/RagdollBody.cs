using System.Numerics;
using System.Runtime.InteropServices;
using Novolis.Physics.Collision.Simple;
using Novolis.Physics.Joints;
using Novolis.Simulation.World.Builders;

namespace RagdollPlay.Game;

/// <summary>Sphere-chain humanoid with distance joints (no angular limits yet).</summary>
internal sealed class RagdollBody
{
    public const float SphereRadius = 0.22f;

    private readonly ConstrainedSphereSimulator _simulator = new()
    {
        Options = { Radius = SphereRadius },
        JointIterations = 12,
    };

    private readonly List<SphereState> _spheres = [];
    private readonly List<DistanceJoint> _joints = [];
    private InteriorClampVolume _clamp;

    public IReadOnlyList<SphereState> Spheres => _spheres;
    public IReadOnlyList<DistanceJoint> Joints => _joints;
    public int LastJointCorrections => _simulator.LastJointCorrections;

    public void SpawnStanding(Vector3 feetPosition, PlayRoom room)
    {
        _spheres.Clear();
        _joints.Clear();
        _clamp = room.InteriorBounds.ToInteriorClamp();

        var hip = feetPosition + new Vector3(0f, 0.95f, 0f);
        var chest = hip + new Vector3(0f, 0.55f, 0f);
        var head = chest + new Vector3(0f, 0.45f, 0f);
        var lShoulder = chest + new Vector3(-0.35f, 0.15f, 0f);
        var rShoulder = chest + new Vector3(0.35f, 0.15f, 0f);
        var lHand = lShoulder + new Vector3(-0.35f, -0.35f, 0f);
        var rHand = rShoulder + new Vector3(0.35f, -0.35f, 0f);
        var lKnee = feetPosition + new Vector3(-0.18f, 0.45f, 0f);
        var rKnee = feetPosition + new Vector3(0.18f, 0.45f, 0f);

        AddSphere(feetPosition);
        AddSphere(lKnee);
        AddSphere(rKnee);
        AddSphere(hip);
        AddSphere(chest);
        AddSphere(head);
        AddSphere(lShoulder);
        AddSphere(rShoulder);
        AddSphere(lHand);
        AddSphere(rHand);

        Link(0, 1);
        Link(0, 2);
        Link(1, 3);
        Link(2, 3);
        Link(3, 4);
        Link(4, 5);
        Link(4, 6);
        Link(4, 7);
        Link(6, 8);
        Link(7, 9);

        _simulator.SetJoints(CollectionsMarshal.AsSpan(_joints));
        _simulator.DepenetrateSpawnedRange(_spheres, 0, _spheres.Count, _clamp);
    }

    public void ApplyImpulse(int sphereIndex, Vector3 impulse)
    {
        if ((uint)sphereIndex >= (uint)_spheres.Count)
            return;

        _spheres[sphereIndex].Velocity += impulse;
        _spheres[sphereIndex].IsSleeping = false;
    }

    public void Step(BvhStaticWorld world, float deltaSeconds) =>
        _simulator.Step(world, _spheres, _clamp, deltaSeconds);

    private void AddSphere(Vector3 position) =>
        _spheres.Add(new SphereState(position, Vector3.Zero));

    private void Link(int a, int b)
    {
        var rest = Vector3.Distance(_spheres[a].Position, _spheres[b].Position);
        _joints.Add(new DistanceJoint(a, b, rest));
    }
}
