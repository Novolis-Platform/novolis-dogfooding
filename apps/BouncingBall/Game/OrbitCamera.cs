using System.Numerics;
using Novolis.Raylib.Game;
using Novolis.Simulation.View;
using RayCamera = Novolis.Raylib.Rendering.Camera;

namespace BouncingBall.Game;

internal sealed class OrbitCamera
{
    private const float MouseSensitivity = 0.003f;
    private const float OrbitDistance = 10f;

    private readonly YawPitchController _controller = new();
    private Vector3 _target;

    public OrbitCamera()
    {
        _controller.Yaw = 0.6f;
        _controller.Pitch = 0.35f;
    }

    public void ResetLook()
    {
        _controller.Yaw = 0.6f;
        _controller.Pitch = 0.35f;
    }

    public void Update(RayGameContext ctx)
    {
        var delta = ctx.MouseDelta;
        _controller.AddLookDelta(-delta.X * MouseSensitivity, -delta.Y * MouseSensitivity);
    }

    public void Follow(Vector3 target) => _target = target;

    public RayCamera BuildRaylibCamera()
    {
        var cosP = MathF.Cos(_controller.Pitch);
        var offset = new Vector3(
            MathF.Sin(_controller.Yaw) * cosP * OrbitDistance,
            MathF.Sin(_controller.Pitch) * OrbitDistance,
            MathF.Cos(_controller.Yaw) * cosP * OrbitDistance);
        var eye = _target + offset;
        return RayCamera.Perspective(eye, _target, Vector3.UnitY, 60f);
    }
}
