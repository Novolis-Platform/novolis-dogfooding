using Novolis.Astro.Abstractions;
using Novolis.Astro.Assessment;
using Novolis.Astro.Catalog;
using Novolis.Astro.Routing;

namespace NearSolPolity;

/// <summary>Deterministic role assignment for the near-Sol polity.</summary>
internal static class RoleAssigner
{
  public const int TargetInhabited = 16;
  public const int TargetIndustrial = 5;
  public const int TargetMining = 10;
  public const int TargetTransit = 20;

  public static IReadOnlyDictionary<string, SystemRole> Assign(StarCatalog catalog, RouteGraph graph)
  {
    var hab = new HabitabilityAssessor();
    var strat = new StrategicValueAssessor();
    var systems = catalog.All.ToList();
    var roles = new Dictionary<string, SystemRole>(StringComparer.OrdinalIgnoreCase);

    foreach (var s in systems)
    {
      roles[s.Id.Value] = SystemRole.Waypoint;
    }

    roles["sol"] = SystemRole.Capital;

    var inhabited = systems
      .Where(s => !s.Id.Value.Equals("sol", StringComparison.OrdinalIgnoreCase))
      .Select(s =>
      {
        var score = hab.Assess(s).Score;
        if (s.Tags.Any(t => t.Equals("planet-host", StringComparison.OrdinalIgnoreCase)))
        {
          score += 25;
        }

        if (s.Tags.Any(t => t.Equals("candidate", StringComparison.OrdinalIgnoreCase)))
        {
          score += 15;
        }

        return (System: s, Score: score);
      })
      .OrderByDescending(x => x.Score)
      .ThenBy(x => x.System.Id.Value, StringComparer.Ordinal)
      .Take(TargetInhabited)
      .Select(x => x.System)
      .ToList();

    foreach (var s in inhabited)
    {
      roles[s.Id.Value] = SystemRole.Inhabited;
    }

    var industrial = inhabited
      .OrderByDescending(s => strat.Assess(s).Score)
      .ThenBy(s => s.Id.Value, StringComparer.Ordinal)
      .Take(TargetIndustrial)
      .ToList();

    foreach (var s in industrial)
    {
      roles[s.Id.Value] = SystemRole.Industrial;
    }

    // Prefer a couple of capital-adjacent industrials if Sol's neighbors scored high.
    if (!industrial.Any() && inhabited.Count > 0)
    {
      roles[inhabited[0].Id.Value] = SystemRole.Industrial;
    }

    var mining = systems
      .Where(s => roles[s.Id.Value] is SystemRole.Waypoint or SystemRole.Transit)
      .Where(s => s.SpectralClass is SpectralClass.M or SpectralClass.K)
      .OrderBy(s => s.Coords.DistanceFromOrigin)
      .ThenBy(s => s.Id.Value, StringComparer.Ordinal)
      .Take(TargetMining)
      .ToList();

    foreach (var s in mining)
    {
      roles[s.Id.Value] = SystemRole.Mining;
    }

    var degree = systems.ToDictionary(
      s => s.Id.Value,
      s => graph.Adjacency.TryGetValue(s.Id.Value, out var edges) ? edges.Count : 0,
      StringComparer.OrdinalIgnoreCase);

    var transit = systems
      .Where(s => roles[s.Id.Value] == SystemRole.Waypoint)
      .Select(s =>
      {
        var deg = degree[s.Id.Value];
        var score = strat.Assess(s).Score + deg * 2.0;
        return (System: s, Score: score, Deg: deg);
      })
      .OrderByDescending(x => x.Score)
      .ThenBy(x => x.System.Id.Value, StringComparer.Ordinal)
      .Take(TargetTransit)
      .ToList();

    foreach (var t in transit)
    {
      roles[t.System.Id.Value] = SystemRole.Transit;
    }

    return roles;
  }

  public static string Summarize(IReadOnlyDictionary<string, SystemRole> roles)
  {
    string C(SystemRole r) => roles.Values.Count(v => v == r).ToString();
    return $"C{C(SystemRole.Capital)} I{C(SystemRole.Inhabited)} Ind{C(SystemRole.Industrial)} M{C(SystemRole.Mining)} T{C(SystemRole.Transit)} W{C(SystemRole.Waypoint)}";
  }
}
