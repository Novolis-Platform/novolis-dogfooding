using Novolis.Rendering.Runtime;
using Novolis.Simulation.View;

namespace Novolis.Dogfooding.Compose;

/// <summary>App-layer bridge from <see cref="ViewPose"/> to <see cref="CameraSnapshot"/> (no Simulation↔Rendering package ref).</summary>
public static class ViewPoseRenderingBridge
{
    public static CameraSnapshot ToCameraSnapshot(this ViewPose pose, float aspectRatio) =>
        CameraSnapshot.LookAt(
            pose.Position,
            pose.Target,
            pose.Up,
            pose.FieldOfViewDegrees,
            aspectRatio);
}
