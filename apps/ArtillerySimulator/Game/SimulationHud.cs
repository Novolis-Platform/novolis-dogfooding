using System.Drawing;
using Novolis.Raylib.Game;

namespace ArtillerySimulator.Game;

internal static class SimulationHud
{
  public static void Draw(
    RayGameContext ctx,
    GunModel gun,
    TerrainWorld terrain,
    ProjectileRun shot,
    float fps)
  {
    const int x = 16;
    const int y = 52;
    const int w = 400;
    const int lineH = 17;
    var lines = 14;
    var h = lineH * lines + 12;

    var text = Color.FromArgb(255, 200, 215, 195);
    var accent = Color.FromArgb(255, 140, 200, 170);
    var dim = Color.FromArgb(255, 120, 130, 125);

    ctx.HudRect(x - 4, y - 4, w + 8, h + 8, Color.FromArgb(220, 8, 12, 14));
    ctx.HudRect(x, y, w, h, Color.FromArgb(200, 18, 24, 28));

    var row = y + 6;
    ctx.HudText("ARTILLERY SIMULATOR", x + 8, row, 14, accent);
    row += lineH;
    ctx.HudText("Physics education demo — not a war game", x + 8, row, 12, dim);
    row += lineH;
    ctx.HudText($"FPS {fps:F0}", x + 8, row, 14, text);
    row += lineH;
    var mils = gun.ElevationDegrees * 6400f / 360f;
    ctx.HudText(
      $"Elev {gun.ElevationDegrees:F1} deg ({mils:F0} mils)  Az {gun.AzimuthDegrees:F0} deg",
      x + 8,
      row,
      14,
      text);
    row += lineH;
    ctx.HudText(
      $"Charge {gun.ChargeIndex + 1}  Mv {gun.MuzzleSpeedMps:F0} m/s  Drag {(gun.DragEnabled ? "on" : "vacuum")}",
      x + 8,
      row,
      14,
      text);
    row += lineH;
    ctx.HudText($"Terrain {(terrain.IsFlat ? "flat" : "hills")}  State {shot.Phase}", x + 8, row, 14, text);
    row += lineH;

    if (shot.Impact is { } impact)
    {
      ctx.HudText(
        $"Impact range {impact.HorizontalRangeMeters:F0} m  TOF {impact.TimeSeconds:F2} s",
        x + 8,
        row,
        14,
        text);
      row += lineH;
      ctx.HudText(
        $"Impact speed {impact.ImpactSpeedMps:F0} m/s  at ({impact.Position.X:F0}, {impact.Position.Y:F0}, {impact.Position.Z:F0})",
        x + 8,
        row,
        14,
        text);
    }
    else
    {
      ctx.HudText("Impact —", x + 8, row, 14, dim);
      row += lineH;
      row += lineH;
    }

    row += lineH;
    ctx.HudText("W/S elev  A/P az  1-3 charge  D drag  Space fire  F flat  R reset", x + 8, row, 12, dim);
  }
}
