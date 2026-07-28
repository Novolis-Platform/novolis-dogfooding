using System.Collections.Immutable;
using Novolis.Astro.Catalog;
using Novolis.Economy;
using Novolis.Economy.Logistics;
using Novolis.Economy.Population;
using Novolis.Economy.Production;
using Novolis.Economy.Simulation;

namespace NearSolPolity;

/// <summary>Seeds the near-Sol polity economy on top of the Astro bridge.</summary>
internal static class PolityWorld
{
  public const decimal OreBuy = 2m;
  /// <summary>Consumer-facing ore ask (rarely used; cohorts prefer goods/parts).</summary>
  public const decimal OreSell = 18m;
  /// <summary>B2B delivered ore price paid by polity to tramp (mine gate + thin freight).</summary>
  public const decimal OreFreightUnit = 6m;
  /// <summary>Parts per ore unit (mine maintenance).</summary>
  public const decimal PartsPerOre = 0.15m;
  /// <summary>Ore per fuel unit (bunker refining at industrial hubs).</summary>
  public const decimal OrePerFuel = 0.5m;
  /// <summary>B2B parts delivered to mines (gate + thin freight).</summary>
  public const decimal PartsFreightUnit = 6m;
  public const decimal PartsBuy = 4m;
  public const decimal PartsSell = 16m;
  public const decimal GoodsSell = 22m;
  public const decimal FuelUnitCost = 1m;
  public const decimal MinMargin = 25m;
  /// <summary>Stop mining when local ore stock exceeds this (avoids endless piles).</summary>
  public const decimal MineOreCap = 80m;
  public const decimal MinePartsFloor = 8m;
  public const decimal PlantOreFloor = 20m;
  /// <summary>Soft floor — emergency fuel still allowed below this.</summary>
  public const decimal PolityCashFloor = 1_000m;

  /// <summary>Opening firm cash + household credits (closed liquid stock at t0).</summary>
  public const decimal OpeningPolityCash = 45_000m;
  public const decimal OpeningTrampCash = 15_000m;
  public const decimal OpeningHouseholdCredits = 20_000m;

  // Short-band ≤10 ly @ 7 d/ly → 1680h; burn ≈ ly/2 so tank 6 covers short hops.
  // burn = hours × difficulty × rate → 10 × 168 × rate ≈ 5 ⇒ rate = 1/336.
  public const decimal FuelBurnPerDifficultyHour = 1m / 336m;

  internal sealed class Site
  {
    public required AstroEconomyBridge.HubBinding Hub { get; init; }
    public FacilityId? PolityFacility { get; init; }
    public FacilityId? TrampPost { get; init; }
    public OperatingUnitId? MfgUnit { get; init; }
  }

  internal sealed class Ids
  {
    public required FirmId Polity { get; init; }
    public required FirmId Tramp { get; init; }
    public required ProductId Ore { get; init; }
    public required ProductId Parts { get; init; }
    public required ProductId Goods { get; init; }
    public required ProductId Fuel { get; init; }
    public required ProductCategoryId OreCat { get; init; }
    public required ProductCategoryId PartsCat { get; init; }
    public required ProductCategoryId GoodsCat { get; init; }
    public required VehicleClassId HullId { get; init; }
    public required VehicleClass Hull { get; init; }
    public required AstroEconomyBridge.BridgeResult Bridge { get; init; }
    public required IReadOnlyDictionary<string, Site> Sites { get; init; }
    public required string RoleSummary { get; init; }
  }

  internal static (EconomySimulation Sim, Ids Ids) Create(ulong seed = 1001)
  {
    var catalog = NearSolCatalog.Load();
    var polity = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a1"));
    var tramp = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a2"));
    var builder = new EconomyWorldBuilder(new EconomyPolicy
    {
      WageRatePerHour = Money.From(3m),
      LaborHoursPerOutputUnit = 0.05m,
      PeriodHours = 24,
      HouseholdCreditFromWages = true,
      CohortBudgetResetMode = CohortBudgetResetMode.CarryForward,
      TollBeneficiaryFirmId = polity,
    });

    var bridge = AstroEconomyBridge.Build(catalog, builder);
    var roleSummary = RoleAssigner.Summarize(bridge.Roles);

    var oreCat = ProductCategoryId.From(builder.NextGuid());
    var partsCat = ProductCategoryId.From(builder.NextGuid());
    var goodsCat = ProductCategoryId.From(builder.NextGuid());
    var fuelCat = ProductCategoryId.From(builder.NextGuid());
    var ore = ProductId.From(builder.NextGuid());
    var parts = ProductId.From(builder.NextGuid());
    var goods = ProductId.From(builder.NextGuid());
    var fuel = ProductId.From(builder.NextGuid());
    var process = ProductionProcessId.From(Guid.Parse("00000000-0000-4000-8000-000000000099"));
    var hullId = VehicleClassId.From(builder.NextGuid());
    var area = GeographicAreaId.From(builder.NextGuid());

    // Circular recipes: parts maintain mines → ore → parts/goods/fuel.
    var oreDef = new ProductDefinition(
      ore, oreCat,
      ImmutableArray.Create(new ProductInput(parts, Quantity.From(PartsPerOre))),
      ImmutableArray<ProductAttributeDefinition>.Empty, process, null);
    var partsDef = new ProductDefinition(
      parts, partsCat, ImmutableArray.Create(new ProductInput(ore, Quantity.From(1m))),
      ImmutableArray<ProductAttributeDefinition>.Empty, process, null);
    var goodsDef = new ProductDefinition(
      goods, goodsCat, ImmutableArray.Create(new ProductInput(parts, Quantity.From(1m))),
      ImmutableArray<ProductAttributeDefinition>.Empty, process, null);
    var fuelDef = new ProductDefinition(
      fuel, fuelCat, ImmutableArray.Create(new ProductInput(ore, Quantity.From(OrePerFuel))),
      ImmutableArray<ProductAttributeDefinition>.Empty, process, null);

    // Tank 6 covers short ≤10 ly (burn ≈5); long band needs transit bunkering.
    // Crew rate kept low so week-scale underways don't dominate margins.
    var hull = new VehicleClass(
      hullId,
      Quantity.From(30m),
      FuelBurnPerDifficultyHour,
      CrewLaborPerUnderwayHour: 0.02m,
      FuelTankCapacity: Quantity.From(6m));

    builder
      .AddProduct(oreDef)
      .AddProduct(partsDef)
      .AddProduct(goodsDef)
      .AddProduct(fuelDef)
      .AddFirm(polity, "Near-Sol Co-op", Money.From(OpeningPolityCash))
      .AddFirm(tramp, "MV Independent", Money.From(OpeningTrampCash))
      .AddVehicleClass(hull)
      .SetTransportFuel(fuel, Money.From(FuelUnitCost))
      .SetLabor(polity, 48m)
      .SetLabor(tramp, 16m);

    var sites = new Dictionary<string, Site>(StringComparer.OrdinalIgnoreCase);
    var householdSeeds = new List<(AstroEconomyBridge.HubBinding Hub, int Pop, SystemRole Role)>();

    foreach (var hub in bridge.Hubs.OrderBy(h => h.SystemId, StringComparer.Ordinal))
    {
      FacilityId? polityFacility = null;
      FacilityId? trampPost = null;
      OperatingUnitId? mfg = null;

      if (hub.Role is SystemRole.Mining or SystemRole.Industrial or SystemRole.Inhabited or SystemRole.Capital)
      {
        var facilityId = FacilityId.From(builder.NextGuid());
        var unitId = OperatingUnitId.From(builder.NextGuid());
        mfg = unitId;
        var kind = hub.Role is SystemRole.Mining or SystemRole.Industrial
          ? OperatingUnitKind.Manufacturing
          : OperatingUnitKind.Storage;
        var capacity = hub.Role switch
        {
          SystemRole.Capital => 200m,
          SystemRole.Industrial => 120m,
          SystemRole.Mining => 80m,
          _ => 60m,
        };
        var layout = new FacilityLayout(
          ImmutableDictionary<OperatingUnitId, OperatingUnit>.Empty
            .Add(unitId, new OperatingUnit(unitId, kind, Quantity.From(capacity))),
          ImmutableArray<MaterialRoute>.Empty);
        builder.AddFacility(new FacilityBinding(facilityId, polity, hub.LocationId, hub.LocationId, layout));
        polityFacility = facilityId;
      }

      if (hub.Role is SystemRole.Capital or SystemRole.Inhabited or SystemRole.Industrial or SystemRole.Mining)
      {
        var postId = FacilityId.From(builder.NextGuid());
        var store = OperatingUnitId.From(builder.NextGuid());
        var layout = new FacilityLayout(
          ImmutableDictionary<OperatingUnitId, OperatingUnit>.Empty
            .Add(store, new OperatingUnit(store, OperatingUnitKind.Storage, Quantity.From(80m))),
          ImmutableArray<MaterialRoute>.Empty);
        builder.AddFacility(new FacilityBinding(postId, tramp, hub.LocationId, hub.LocationId, layout));
        trampPost = postId;
      }

      if (hub.Role is SystemRole.Capital or SystemRole.Inhabited or SystemRole.Industrial)
      {
        var pop = hub.Role switch
        {
          SystemRole.Capital => 400,
          SystemRole.Industrial => 180,
          _ => 120,
        };
        householdSeeds.Add((hub, pop, hub.Role));
      }

      sites[hub.SystemId] = new Site
      {
        Hub = hub,
        PolityFacility = polityFacility,
        TrampPost = trampPost,
        MfgUnit = mfg,
      };
    }

    // Finite household float carved from opening money stock (not reminted each day).
    var popSum = householdSeeds.Sum(h => h.Pop);
    var creditsLeft = OpeningHouseholdCredits;
    for (var i = 0; i < householdSeeds.Count; i++)
    {
      var (hub, pop, role) = householdSeeds[i];
      decimal budget;
      if (i == householdSeeds.Count - 1)
      {
        budget = creditsLeft;
      }
      else
      {
        budget = Math.Round(OpeningHouseholdCredits * pop / popSum, 2, MidpointRounding.AwayFromZero);
        creditsLeft -= budget;
      }

      var prefs = role == SystemRole.Industrial
        ? ImmutableArray.Create(
          new CategoryPreference(partsCat, 0.4m),
          new CategoryPreference(goodsCat, 0.6m))
        : ImmutableArray.Create(
          new CategoryPreference(goodsCat, 0.7m),
          new CategoryPreference(partsCat, 0.3m));

      builder.AddCohort(new ConsumerCohort(
        ConsumerCohortId.From(builder.NextGuid()),
        new PopulationCount(pop),
        Money.From(budget),
        new PreferenceProfile(prefs, 0.7m, 0m, 0m),
        area));
      _ = hub;
    }

    var ids = new Ids
    {
      Polity = polity,
      Tramp = tramp,
      Ore = ore,
      Parts = parts,
      Goods = goods,
      Fuel = fuel,
      OreCat = oreCat,
      PartsCat = partsCat,
      GoodsCat = goodsCat,
      HullId = hullId,
      Hull = hull,
      Bridge = bridge,
      Sites = sites,
      RoleSummary = roleSummary,
    };

    var sim = new EconomySimulation(seed, builder.Build());
    SeedInventory(sim, ids);
    return (sim, ids);
  }

  private static void SeedInventory(EconomySimulation sim, Ids ids)
  {
    var inv = sim.State.World.Inventory;
    var epoch = SimulationDate.Epoch;

    void Add(FirmId firm, InventoryLocationId loc, ProductId product, decimal qty, decimal unitCost)
    {
      if (qty <= 0)
      {
        return;
      }

      inv.Add(
        new InventoryKey(firm, loc, product),
        new ProductBatch(product, Quantity.From(qty), new ProductQuality(100m), Money.From(unitCost), epoch, null));
    }

    foreach (var site in ids.Sites.Values)
    {
      var hub = site.Hub;
      switch (hub.Role)
      {
        case SystemRole.Mining:
          Add(ids.Polity, hub.LocationId, ids.Ore, 30m, OreBuy);
          Add(ids.Polity, hub.LocationId, ids.Parts, 25m, PartsBuy); // maintenance float
          Add(ids.Polity, hub.LocationId, ids.Fuel, 20m, FuelUnitCost);
          break;
        case SystemRole.Industrial:
          Add(ids.Polity, hub.LocationId, ids.Ore, 40m, OreBuy);
          Add(ids.Polity, hub.LocationId, ids.Parts, 30m, PartsBuy);
          Add(ids.Polity, hub.LocationId, ids.Goods, 15m, GoodsSell * 0.4m);
          Add(ids.Polity, hub.LocationId, ids.Fuel, 30m, FuelUnitCost);
          break;
        case SystemRole.Capital:
          Add(ids.Polity, hub.LocationId, ids.Parts, 20m, PartsBuy);
          Add(ids.Polity, hub.LocationId, ids.Goods, 25m, GoodsSell * 0.4m);
          Add(ids.Polity, hub.LocationId, ids.Fuel, 40m, FuelUnitCost);
          Add(ids.Tramp, hub.LocationId, ids.Fuel, 12m, FuelUnitCost);
          break;
        case SystemRole.Inhabited:
          Add(ids.Polity, hub.LocationId, ids.Goods, 10m, GoodsSell * 0.4m);
          Add(ids.Polity, hub.LocationId, ids.Fuel, 15m, FuelUnitCost);
          break;
        case SystemRole.Transit:
          Add(ids.Polity, hub.LocationId, ids.Fuel, 35m, FuelUnitCost);
          break;
        default:
          Add(ids.Polity, hub.LocationId, ids.Fuel, 8m, FuelUnitCost);
          break;
      }
    }
  }
}
