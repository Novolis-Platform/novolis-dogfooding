using System.Collections.Immutable;
using Novolis.Astro.Catalog;
using Novolis.Economy;
using Novolis.Economy.Logistics;
using Novolis.Economy.Population;
using Novolis.Economy.Production;
using Novolis.Economy.Simulation;

namespace NearSolPolity;

/// <summary>Seeds the near-Sol four-firm tycoon economy on the Astro bridge.</summary>
internal static class PolityWorld
{
  public const decimal OreBuy = 2m;
  public const decimal OreSell = 12m;
  /// <summary>Industry delivered Raw bid — wide enough vs mine gate to clear haul + MinMargin.</summary>
  public const decimal OreDelivered = 8m;
  public const decimal FreightPremiumPerUnit = 1m;
  public const decimal PartsPerOre = 0.15m;
  public const decimal OrePerFuel = 0.5m;
  public const decimal PartsBuy = 4m;
  public const decimal PartsSell = 10m;
  public const decimal PartsDelivered = 7m;
  public const decimal GoodsFactory = 6m;
  public const decimal GoodsSell = 12m;
  public const decimal GoodsDelivered = 11m;
  public const decimal FuelUnitCost = 1m;
  public const decimal MinMargin = 5m;
  public const decimal MineOreCap = 80m;
  public const decimal MinePartsFloor = 8m;
  public const decimal PlantOreFloor = 20m;
  public const decimal RetailStockTarget = 20m;
  public const decimal FirmCashFloor = 1_000m;

  public const decimal OpeningFirmCash = 15_000m;
  public const decimal OpeningHouseholdCredits = 20_000m;

  public const decimal FuelBurnPerDifficultyHour = 1m / 62.4m;

  public static string SkuLabel(ProductId product, Ids ids)
  {
    if (product.Equals(ids.Ore)) return "Raw";
    if (product.Equals(ids.Parts)) return "Capital";
    if (product.Equals(ids.Goods)) return "Final";
    if (product.Equals(ids.Fuel)) return "Energy";
    return "sku";
  }

  internal sealed class Site
  {
    public required AstroEconomyBridge.HubBinding Hub { get; init; }
    public FirmId? OwnerFirm { get; init; }
    public FacilityId? Facility { get; init; }
    public FacilityId? CarrierPost { get; init; }
    public OperatingUnitId? MfgUnit { get; init; }
  }

  internal sealed class Ids
  {
    public required FirmId Mining { get; init; }
    public required FirmId Industry { get; init; }
    public required FirmId Station { get; init; }
    public required FirmId Carrier { get; init; }
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

    public IEnumerable<(string Name, FirmId Id)> Firms
    {
      get
      {
        yield return ("Mining", Mining);
        yield return ("Industry", Industry);
        yield return ("Station", Station);
        yield return ("Carrier", Carrier);
      }
    }
  }

  internal static (EconomySimulation Sim, Ids Ids) Create(ulong seed = 1001)
  {
    var catalog = NearSolCatalog.Load();
    var mining = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a1"));
    var industry = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a2"));
    var station = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a3"));
    var carrier = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a4"));

    var builder = new EconomyWorldBuilder(new EconomyPolicy
    {
      WageRatePerHour = Money.From(12m),
      LaborHoursPerOutputUnit = 0.05m,
      PeriodHours = 24,
      HouseholdCreditFromWages = true,
      CohortBudgetResetMode = CohortBudgetResetMode.CarryForward,
      TollBeneficiaryFirmId = station,
      PriceElasticity = 0.8m,
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
    var areas = new Dictionary<string, GeographicAreaId>(StringComparer.OrdinalIgnoreCase);
    foreach (var hub in bridge.Hubs)
    {
      areas[hub.SystemId] = GeographicAreaId.From(builder.NextGuid());
    }

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
      .AddFirm(mining, "Near-Sol Mining", Money.From(OpeningFirmCash))
      .AddFirm(industry, "Near-Sol Industry", Money.From(OpeningFirmCash))
      .AddCivic(station, "Near-Sol Station", Money.From(OpeningFirmCash), "nearsol-civic")
      .AddFirm(carrier, "MV Independent", Money.From(OpeningFirmCash))
      .SetOwnership(mining, station, 1m)
      .SetOwnership(industry, station, 0.4m)
      .AddVehicleClass(hull)
      .SetTransportFuel(fuel, Money.From(FuelUnitCost))
      .SetLabor(mining, 40m)
      .SetLabor(industry, 48m)
      .SetLabor(station, 24m)
      .SetLabor(carrier, 32m);

    var sites = new Dictionary<string, Site>(StringComparer.OrdinalIgnoreCase);
    var householdSeeds = new List<(AstroEconomyBridge.HubBinding Hub, int Pop)>();

    foreach (var hub in bridge.Hubs.OrderBy(h => h.SystemId, StringComparer.Ordinal))
    {
      FirmId? owner = hub.Role switch
      {
        SystemRole.Mining => mining,
        SystemRole.Industrial => industry,
        SystemRole.Capital or SystemRole.Inhabited => station,
        _ => null,
      };

      FacilityId? facility = null;
      OperatingUnitId? mfg = null;
      FacilityId? carrierPost = null;
      var area = areas[hub.SystemId];

      if (owner is { } firmId && hub.Role is SystemRole.Mining or SystemRole.Industrial
          or SystemRole.Inhabited or SystemRole.Capital)
      {
        var facilityId = FacilityId.From(builder.NextGuid());
        var unitId = OperatingUnitId.From(builder.NextGuid());
        mfg = unitId;
        var kind = hub.Role is SystemRole.Mining or SystemRole.Industrial
          ? OperatingUnitKind.Manufacturing
          : OperatingUnitKind.Sales;
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
        builder.AddFacility(new FacilityBinding(facilityId, firmId, hub.LocationId, hub.LocationId, layout, area));
        facility = facilityId;
      }

      if (hub.Role is SystemRole.Capital or SystemRole.Inhabited or SystemRole.Industrial or SystemRole.Mining)
      {
        var postId = FacilityId.From(builder.NextGuid());
        var store = OperatingUnitId.From(builder.NextGuid());
        var layout = new FacilityLayout(
          ImmutableDictionary<OperatingUnitId, OperatingUnit>.Empty
            .Add(store, new OperatingUnit(store, OperatingUnitKind.Storage, Quantity.From(80m))),
          ImmutableArray<MaterialRoute>.Empty);
        builder.AddFacility(new FacilityBinding(postId, carrier, hub.LocationId, hub.LocationId, layout, area));
        carrierPost = postId;
      }

      if (hub.Role is SystemRole.Capital or SystemRole.Inhabited or SystemRole.Industrial)
      {
        var pop = hub.Role switch
        {
          SystemRole.Capital => 400,
          SystemRole.Industrial => 180,
          _ => 120,
        };
        householdSeeds.Add((hub, pop));
      }

      sites[hub.SystemId] = new Site
      {
        Hub = hub,
        OwnerFirm = owner,
        Facility = facility,
        CarrierPost = carrierPost,
        MfgUnit = mfg,
      };
    }

    var popSum = householdSeeds.Sum(h => h.Pop);
    var creditsLeft = OpeningHouseholdCredits;
    for (var i = 0; i < householdSeeds.Count; i++)
    {
      var (hub, pop) = householdSeeds[i];
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

      var prefs = ImmutableArray.Create(
        new CategoryPreference(goodsCat, 0.85m),
        new CategoryPreference(partsCat, 0.15m));

      builder.AddCohort(new ConsumerCohort(
        ConsumerCohortId.From(builder.NextGuid()),
        new PopulationCount(pop),
        Money.From(budget),
        new PreferenceProfile(prefs, 0.7m, 0m, 0m),
        areas[hub.SystemId]));
    }

    var ids = new Ids
    {
      Mining = mining,
      Industry = industry,
      Station = station,
      Carrier = carrier,
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
          Add(ids.Mining, hub.LocationId, ids.Ore, 30m, OreBuy);
          Add(ids.Mining, hub.LocationId, ids.Parts, 25m, PartsBuy);
          Add(ids.Station, hub.LocationId, ids.Fuel, 20m, FuelUnitCost);
          Add(ids.Carrier, hub.LocationId, ids.Fuel, 10m, FuelUnitCost);
          break;
        case SystemRole.Industrial:
          Add(ids.Industry, hub.LocationId, ids.Ore, 40m, OreBuy);
          Add(ids.Industry, hub.LocationId, ids.Parts, 30m, PartsBuy);
          Add(ids.Industry, hub.LocationId, ids.Goods, 15m, GoodsSell * 0.4m);
          Add(ids.Station, hub.LocationId, ids.Fuel, 30m, FuelUnitCost);
          Add(ids.Carrier, hub.LocationId, ids.Fuel, 10m, FuelUnitCost);
          break;
        case SystemRole.Capital:
          Add(ids.Station, hub.LocationId, ids.Parts, 20m, PartsBuy);
          Add(ids.Station, hub.LocationId, ids.Goods, 25m, GoodsSell * 0.4m);
          Add(ids.Station, hub.LocationId, ids.Fuel, 40m, FuelUnitCost);
          Add(ids.Carrier, hub.LocationId, ids.Fuel, 12m, FuelUnitCost);
          break;
        case SystemRole.Inhabited:
          Add(ids.Station, hub.LocationId, ids.Goods, 10m, GoodsSell * 0.4m);
          Add(ids.Station, hub.LocationId, ids.Fuel, 15m, FuelUnitCost);
          Add(ids.Carrier, hub.LocationId, ids.Fuel, 8m, FuelUnitCost);
          break;
        case SystemRole.Transit:
          Add(ids.Station, hub.LocationId, ids.Fuel, 35m, FuelUnitCost);
          Add(ids.Carrier, hub.LocationId, ids.Fuel, 10m, FuelUnitCost);
          break;
        default:
          Add(ids.Station, hub.LocationId, ids.Fuel, 8m, FuelUnitCost);
          Add(ids.Carrier, hub.LocationId, ids.Fuel, 6m, FuelUnitCost);
          break;
      }
    }
  }
}
