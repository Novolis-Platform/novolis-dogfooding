using System.Drawing;
using System.Numerics;
using Novolis.Raylib.Game;
using Novolis.Raylib.Interact;

namespace BouncingBall.Game;

internal sealed class BouncingBallGame
{
    private static readonly Color Background = Color.FromArgb(255, 18, 22, 32);
    private static readonly Color BallFill = Color.FromArgb(255, 90, 200, 255);
    private static readonly Color BallWire = Color.FromArgb(255, 200, 240, 255);
    private static readonly Color WallWire = Color.FromArgb(255, 70, 90, 120);
    private static readonly Color HudText = Color.FromArgb(255, 210, 220, 235);

    private RoomWorld _room = null!;
    private BallSimulation _ball = null!;
    private readonly OrbitCamera _camera = new();

    public void Initialize(RayGameContext ctx)
    {
        _room = RoomWorld.Create();
        _ball = BallSimulation.CreateDefault(_room);
        _camera.Follow(_ball.Position);
    }

    public void Update(RayGameContext ctx)
    {
        if (ctx.IsKeyPressed(KeyboardKey.R))
        {
            _ball.Reset(_room);
            _camera.ResetLook();
        }

        _camera.Update(ctx);
        _ball.Step(ctx.DeltaSeconds);
        _camera.Follow(_ball.Position);

        ctx.Clear(Background);
        var camera = _camera.BuildRaylibCamera();
        ctx.BeginWorld(camera);
        DrawWalls(ctx);
        ctx.DrawGlowSphere(_ball.Position, BallSimulation.Radius, BallFill);
        ctx.DrawGlowSphereWires(_ball.Position, BallSimulation.Radius, BallWire);
        ctx.EndWorld();

        ctx.Text($"Speed: {_ball.Speed:F2} m/s  |  g + air drag  |  Drag: orbit  |  R: reset", 16, 16, 20, HudText);
    }

    private void DrawWalls(RayGameContext ctx)
    {
        var h = RoomWorld.WallHeight * 0.5f;
        for (var y = 0u; y < RoomWorld.GridSize; y++)
        for (var x = 0u; x < RoomWorld.GridSize; x++)
        {
            if (_room.Walls[x, y, 0] == 0)
                continue;

            var cx = (x + 0.5f) * RoomWorld.CellSize;
            var cz = (y + 0.5f) * RoomWorld.CellSize;
            ctx.DrawShipWires(
                new Vector3(cx, h, cz),
                new Vector3(RoomWorld.CellSize, RoomWorld.WallHeight, RoomWorld.CellSize),
                WallWire);
        }
    }
}
