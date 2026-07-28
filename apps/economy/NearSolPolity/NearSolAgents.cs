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
      BaseOutputRate: 3.5m, OutputCap: PolityWorld.MineOreCap,
      InputPerOutput: PolityWorld.PartsPerOre, InputFloor: PolityWorld.MinePartsFloor,
      SellAboveStock: 8m, SellKeepFloor: 4m, SellMaxQty: 30m,
      OutputGatePrice: PolityWorld.OreBuy, InputLimitPrice: PolityWorld.PartsDelivered));

    var industry = new ManufacturingFirmAgent(ids.Industry, new ManufacturingFirmAgentPolicy(
      plantSites, ids.Ore, PolityWorld.PlantOreFloor + 12m, PolityWorld.OreDelivered,
      [
        new ManufacturedSkuPolicy(
          ids.Parts, BaseRate: 6m, StockTarget: 55m, MinInputOnHand: 1m, RequiredInput: ids.Ore,
          SellAboveStock: 4m, SellKeepFloor: 2m, SellMaxQty: 22m, GatePrice: PolityWorld.PartsBuy),
        new ManufacturedSkuPolicy(
          ids.Goods, BaseRate: 3m, StockTarget: PolityWorld.RetailStockTarget * 1.4m,
          MinInputOnHand: 1m, RequiredInput: ids.Parts,
          SellAboveStock: 5m, SellKeepFloor: 2m, SellMaxQty: 14m, GatePrice: PolityWorld.GoodsFactory),
        new ManufacturedSkuPolicy(
          ids.Fuel, BaseRate: 2.5m, StockTarget: 36m, MinInputOnHand: 8m, RequiredInput: ids.Ore,
          SellAboveStock: 8m, SellKeepFloor: 3m, SellMaxQty: 14m, GatePrice: PolityWorld.FuelUnitCost),
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

    // Home hubs: cycle Sol / mines / plants so the larger tramp fleet fans out.
    var homeHubs = new List<TransportHubId>();
    var mineHomes = miningSites.Where(s => s.HubId is not null).Select(s => s.HubId!.Value).ToList();
    var plantHomes = plantSites.Where(s => s.HubId is not null).Select(s => s.HubId!.Value).ToList();
    var pool = new List<TransportHubId> { ids.Sites["sol"].Hub.HubId };
    pool.AddRange(mineHomes);
    pool.AddRange(plantHomes);
    if (pool.Count == 0)
    {
      pool.Add(ids.Sites["sol"].Hub.HubId);
    }

    for (var i = 0; i < ids.Carriers.Count; i++)
    {
      homeHubs.Add(pool[i % pool.Count]);
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
      CashFloorToLend: 5_000m,
      BorrowerCashFloor: PolityWorld.FirmCashFloor + 1_000m,
      LoanPrincipal: Money.From(900m),
      AnnualInterestRate: 0.08m,
      TermHours: SimulationHour.HoursPerDay * 60,
      MaxActiveLoansToBorrower: 2));

    sim.Enqueue(new OriginateLoan(
      ids.Station, ids.Industry, Money.From(800m), 0.08m, SimulationHour.HoursPerDay * 90));

    var households = sim.State.World.Cohorts
      .Where(c => c.Definition.HouseholdFirmId is not null)
      .OrderBy(c => c.Definition.Id.Value)
      .Select(c => new HouseholdFirmAgent(
        c.Definition.HouseholdFirmId!.Value,
        new HouseholdFirmAgentPolicy(
          // Invest into Mining float; leave working-capital credit to Civic treasury.
          PreferredBorrower: null,
          PreferredIssuer: ids.Mining,
          PurchaseFraction: 0.01m,
          PurchasePrice: Money.From(40m),
          MaxActiveLoans: 1)))
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
