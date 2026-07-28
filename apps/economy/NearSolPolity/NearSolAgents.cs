using Novolis.Economy;
using Novolis.Economy.Agents;
using Novolis.Economy.Logistics;
using Novolis.Economy.Simulation;

namespace NearSolPolity;

/// <summary>Builds library economic agents from NearSol seed (Astro stays in the app).</summary>
internal static class NearSolAgents
{
  public sealed class Bundle
  {
    public required ExtractiveFirmAgent Mining { get; init; }
    public required ManufacturingFirmAgent Industry { get; init; }
    public required RetailFirmAgent Station { get; init; }
    public required CarrierFirmAgent Carrier { get; init; }
    public required IReadOnlyList<CarrierFirmAgent> Carriers { get; init; }
    public required TreasuryFirmAgent Treasury { get; init; }
    public required IReadOnlyList<HouseholdFirmAgent> Households { get; init; }
    public required IReadOnlyList<IEconomicAgent> PulseOrder { get; init; }
  }

  public static Bundle Create(EconomySimulation sim, PolityWorld.Ids ids)
  {
    AgentSite Site(PolityWorld.Site s) => new(
      s.Hub.LocationId, s.Facility, s.Hub.HubId, s.Hub.Name);

    var miningSites = ids.Sites.Values
      .Where(s => s.Hub.Role == SystemRole.Mining && s.Facility is not null)
      .Select(Site)
      .ToList();
    var plantSites = ids.Sites.Values
      .Where(s => s.Hub.Role == SystemRole.Industrial && s.Facility is not null)
      .Select(Site)
      .ToList();
    var retailSites = ids.Sites.Values
      .Where(s => s.Facility is not null
                  && s.Hub.Role is SystemRole.Capital or SystemRole.Inhabited or SystemRole.Industrial)
      .Select(Site)
      .ToList();
    var bunkerSites = ids.Sites.Values
      .Where(s => s.Hub.Role is SystemRole.Transit or SystemRole.Capital
        or SystemRole.Industrial or SystemRole.Mining)
      .Select(Site)
      .ToList();
    var allSites = ids.Sites.Values.Select(Site).ToList();

    var mining = new ExtractiveFirmAgent(ids.Mining, new ExtractiveFirmAgentPolicy(
      miningSites, ids.Ore, ids.Parts,
      BaseOutputRate: 2m, OutputCap: PolityWorld.MineOreCap,
      InputPerOutput: PolityWorld.PartsPerOre, InputFloor: PolityWorld.MinePartsFloor,
      SellAboveStock: 10m, SellKeepFloor: 5m, SellMaxQty: 20m,
      OutputGatePrice: PolityWorld.OreBuy, InputLimitPrice: PolityWorld.PartsDelivered));

    var industry = new ManufacturingFirmAgent(ids.Industry, new ManufacturingFirmAgentPolicy(
      plantSites, ids.Ore, PolityWorld.PlantOreFloor + 15m, PolityWorld.OreDelivered,
      [
        new ManufacturedSkuPolicy(
          ids.Parts, BaseRate: 4m, StockTarget: 40m, MinInputOnHand: 1m, RequiredInput: ids.Ore,
          SellAboveStock: 8m, SellKeepFloor: 4m, SellMaxQty: 15m, GatePrice: PolityWorld.PartsBuy),
        new ManufacturedSkuPolicy(
          ids.Goods, BaseRate: 2.5m, StockTarget: PolityWorld.RetailStockTarget * 1.5m,
          MinInputOnHand: 1m, RequiredInput: ids.Parts,
          SellAboveStock: 6m, SellKeepFloor: 2m, SellMaxQty: 12m, GatePrice: PolityWorld.GoodsFactory),
        new ManufacturedSkuPolicy(
          ids.Fuel, BaseRate: 2m, StockTarget: 30m, MinInputOnHand: 10m, RequiredInput: ids.Ore,
          SellAboveStock: 10m, SellKeepFloor: 4m, SellMaxQty: 12m, GatePrice: PolityWorld.FuelUnitCost),
      ]));

    var station = new RetailFirmAgent(ids.Station, new RetailFirmAgentPolicy(
      retailSites, bunkerSites,
      [
        new RetailSkuPolicy(
          ids.Goods, PolityWorld.GoodsSell, PolityWorld.RetailStockTarget,
          PolityWorld.GoodsDelivered, PostRetailPrice: true),
        new RetailSkuPolicy(
          ids.Parts, PolityWorld.PartsSell, PolityWorld.RetailStockTarget * 0.5m,
          PolityWorld.PartsDelivered, PostRetailPrice: true),
      ],
      new BunkerSkuPolicy(
        ids.Fuel, MinStock: 10m, BuyLimitPrice: PolityWorld.FuelUnitCost * 1.1m,
        SellPrice: PolityWorld.FuelUnitCost, AllowProcurement: true)));

    decimal Gate(ProductId p)
    {
      if (p.Equals(ids.Ore)) return PolityWorld.OreBuy;
      if (p.Equals(ids.Parts)) return PolityWorld.PartsBuy;
      if (p.Equals(ids.Goods)) return PolityWorld.GoodsFactory;
      return PolityWorld.FuelUnitCost;
    }

    // Home hubs: Sol, first mine, first plant — spreads tramp coverage across the graph.
    var homeHubs = new List<TransportHubId> { ids.Sites["sol"].Hub.HubId };
    var mineHome = miningSites.FirstOrDefault(s => s.HubId is not null)?.HubId;
    var plantHome = plantSites.FirstOrDefault(s => s.HubId is not null)?.HubId;
    if (mineHome is { } mh)
    {
      homeHubs.Add(mh);
    }

    if (plantHome is { } ph)
    {
      homeHubs.Add(ph);
    }

    while (homeHubs.Count < ids.Carriers.Count)
    {
      homeHubs.Add(ids.Sites["sol"].Hub.HubId);
    }

    var trampAgents = new List<CarrierFirmAgent>(ids.Carriers.Count);
    for (var i = 0; i < ids.Carriers.Count; i++)
    {
      trampAgents.Add(new CarrierFirmAgent(
        ids.Carriers[i],
        new CarrierFirmAgentPolicy(
          allSites, [ids.Ore, ids.Parts, ids.Goods], ids.Fuel,
          ids.HullId, ids.Hull, PolityWorld.MinMargin, Gate,
          FuelBuyLimitPrice: PolityWorld.FuelUnitCost * 1.25m),
        homeHubs[i],
        rngSalt: 0x43415252UL ^ (ulong)(i + 1) * 0x9E3779B97F4A7C15UL));
    }

    var treasury = new TreasuryFirmAgent(ids.Station, new TreasuryFirmAgentPolicy(
      [ids.Mining, ids.Industry, .. ids.Carriers],
      CashFloorToLend: 8_000m,
      BorrowerCashFloor: PolityWorld.FirmCashFloor + 2_000m,
      LoanPrincipal: Money.From(500m),
      AnnualInterestRate: 0.08m,
      TermHours: SimulationHour.HoursPerDay * 30));

    // Seed a small working-capital loan so Finance is exercised from hour 0.
    sim.Enqueue(new OriginateLoan(
      ids.Station, ids.Mining, Money.From(400m), 0.08m, SimulationHour.HoursPerDay * 45));

    var households = sim.State.World.Cohorts
      .Where(c => c.Definition.HouseholdFirmId is not null)
      .OrderBy(c => c.Definition.Id.Value)
      .Select(c => new HouseholdFirmAgent(
        c.Definition.HouseholdFirmId!.Value,
        new HouseholdFirmAgentPolicy(
          PreferredBorrower: ids.Industry,
          PreferredIssuer: ids.Mining,
          LoanPrincipal: Money.From(25m),
          PurchaseFraction: 0.005m,
          PurchasePrice: Money.From(20m))))
      .ToList();

    IEconomicAgent[] pulse =
    [
      mining, industry, station, .. trampAgents.Cast<IEconomicAgent>(), treasury,
      .. households.Cast<IEconomicAgent>(),
    ];

    return new Bundle
    {
      Mining = mining,
      Industry = industry,
      Station = station,
      Carrier = trampAgents[0],
      Carriers = trampAgents,
      Treasury = treasury,
      Households = households,
      PulseOrder = pulse,
    };
  }
}
