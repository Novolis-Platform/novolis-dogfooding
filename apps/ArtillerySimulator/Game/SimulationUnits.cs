using System.Numerics;

namespace ArtillerySimulator.Game;

/// <summary>Realistic-scale constants for 155 mm M777-class educational preset (approximate, not classified).</summary>
internal static class SimulationUnits
{
  /// <summary>Local training patch along downrange (m).</summary>
  public const float ExtentMeters = 2500f;

  public const float GunPivotX = 50f;
  public const float GunHeightOffset = 1.6f;

  /// <summary>MACS-style reduced / standard / max educational muzzle velocities (m/s).</summary>
  public static readonly float[] ChargeSpeedsMps = [395f, 560f, 684f];

  public static readonly string[] ChargeLabels = ["reduced", "standard", "max"];

  /// <summary>Rolling hills ~15–45 m relief on a 2.5 km patch.</summary>
  public const float HillBase = 18f;
  public const float HillSin = 12f;
  public const float HillCos = 10f;
  public const float HillMix = 6f;

  public const float FixedCamLookAhead = 400f;
  public static readonly Vector3 FixedCamEyeOffset = new(-180f, 120f, 100f);

  public const float ChaseCamBack = 35f;
  public const float ChaseCamUp = 18f;
  public const float ChaseCamLeadMax = 40f;

  public static string FormatRange(float meters) =>
      meters >= 1000f ? $"{meters / 1000f:F2} km" : $"{meters:F0} m";
}
