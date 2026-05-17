using System.Drawing;
using Novolis.Raylib.Game;

namespace ArtillerySimulator.Game;

internal static class SimulationHud
{
  public static void Draw(
    RayGameContext ctx,
    GunModel gun,
    TerrainWorld terrain,
    AtmosphereModel atmosphere,
    ProjectileRun shot,
    ArtilleryCamera camera,
    float fps)
  {
    const int x = 16;
    const int y = 52;
    const int w = 500;
    const int lineH = 17;
    var lines = shot.Phase == ShotPhase.InFlight ? 19 : 18;
    var h = lineH * lines + 12;

    var text = Color.FromArgb(255, 200, 215, 195);
    var accent = Color.FromArgb(255, 140, 200, 170);
    var dim = Color.FromArgb(255, 120, 130, 125);

    ctx.HudRect(x - 4, y - 4, w + 8, h + 8, Color.FromArgb(220, 8, 12, 14));
    ctx.HudRect(x, y, w, h, Color.FromArgb(200, 18, 24, 28));

    var row = y + 6;
    ctx.HudText("ARTILLERY SIMULATOR", x + 8, row, 14, accent);
    row += lineH;
    ctx.HudText("155 mm M777-class — educational ballistics, not classified data", x + 8, row, 12, dim);
    row += lineH;
    ctx.HudText(
      $"Typical max HE (pubs) {SimulationUnits.ReferenceMaxRangeKmForCharge(0):F0}/{SimulationUnits.ReferenceMaxRangeKmForCharge(1):F1}/{SimulationUnits.ReferenceMaxRangeKmForCharge(2):F0} km",
      x + 8,
      row,
      12,
      dim);
    row += lineH;
    ctx.HudText(
      $"FPS {fps:F0}  Cam {(camera.Mode == CameraMode.Freecam ? "free" : "orbit")}",
      x + 8,
      row,
      14,
      text);
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
      $"Charge {gun.ChargeLabel}  Mv {gun.MuzzleSpeedMps:F0} m/s  Aero {(gun.DragEnabled ? "ρ+wind+RH" : "vacuum")}",
      x + 8,
      row,
      14,
      text);
    row += lineH;
    var sampleAlt = shot.Phase == ShotPhase.InFlight ? shot.CurrentPosition.Y : terrain.GunBaseline.Y;
    ctx.HudText(atmosphere.SummaryLine(sampleAlt), x + 8, row, 13, text);
    row += lineH;
    var terrainLabel = terrain.IsFlat
      ? "flat"
      : terrain.Style switch
      {
        TerrainStyle.AfghanHighland => "afghan highland",
        TerrainStyle.NordicRidges => "nordic ridges",
        _ => "rugged blend",
      };
    ctx.HudText(
      $"Terrain {terrainLabel}  {SimulationUnits.FormatRange(terrain.ExtentMeters)}  State {shot.Phase}",
      x + 8,
      row,
      14,
      text);
    row += lineH;

    if (shot.Phase == ShotPhase.InFlight)
    {
      ctx.HudText(
        $"Alt {shot.CurrentPosition.Y:F0} m  Spd {shot.CurrentVelocity.Length():F0} m/s  T {shot.TimeSeconds:F1} s",
        x + 8,
        row,
        14,
        text);
      row += lineH;
    }

    if (shot.Impact is { } impact)
    {
      var reason = impact.Reason == ImpactEndReason.BeyondRange ? "  Range limit (map edge)" : "";
      ctx.HudText(
        $"Impact {SimulationUnits.FormatRange(impact.HorizontalRangeMeters)}  TOF {impact.TimeSeconds:F1} s{reason}",
        x + 8,
        row,
        14,
        text);
      row += lineH;
      ctx.HudText(
        $"Impact speed {impact.ImpactSpeedMps:F0} m/s  ground ({impact.Position.X:F0}, {impact.Position.Y:F0}, {impact.Position.Z:F0})",
        x + 8,
        row,
        14,
        text);
    }
    else if (shot.Phase != ShotPhase.InFlight)
    {
      ctx.HudText("Impact —", x + 8, row, 14, dim);
      row += lineH;
    }

    row += lineH;
    ctx.HudText(
      "WASD fly  mouse look  Shift/Ctrl elev  Q/E az  C cam  F flat  T terrain  R reseed",
      x + 8,
      row,
      12,
      dim);
  }
}
