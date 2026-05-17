using System.Numerics;
using RayCamera = Novolis.Raylib.Rendering.Camera;

namespace ArtillerySimulator.Game;

internal sealed class OverviewCamera
{
  private readonly Vector3 _eye;
  private readonly Vector3 _target;

  public OverviewCamera()
  {
    _target = new Vector3(TerrainWorld.ExtentMeters * 0.45f, 40f, TerrainWorld.ExtentMeters * 0.5f);
    _eye = _target + new Vector3(-220f, 160f, 140f);
  }

  public RayCamera Build() => RayCamera.Perspective(_eye, _target, Vector3.UnitY, 52f);
}
