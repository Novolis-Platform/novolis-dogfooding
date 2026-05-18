using System.Numerics;

namespace RandoriFight.Game.Skeleton;

/// <summary>World-space joint positions for one solved humanoid pose.</summary>
internal sealed class SkeletonFrame
{
    private readonly Vector3[] _joints = new Vector3[(int)HumanoidBoneId.Count];

    public ReadOnlySpan<Vector3> Joints => _joints;

    public Vector3 this[HumanoidBoneId bone] => _joints[(int)bone];

    public void Set(HumanoidBoneId bone, Vector3 world) => _joints[(int)bone] = world;

    public Vector3 BladeRoot => this[HumanoidBoneId.BladeRoot];
    public Vector3 BladeTip => this[HumanoidBoneId.BladeTip];
}
