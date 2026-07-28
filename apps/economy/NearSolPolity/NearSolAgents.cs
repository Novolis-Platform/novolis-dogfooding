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
    AgentSite MfgSite(PolityWorld.Site s) => new(
      s.Hub.LocationId, s.MfgFacility, s.Hub.HubId, s.Hub.Name);

    var miningSites = ids.Sites.Values
      .Where(s => s.Hub.Role == SystemRole.Mining && s.MfgFacility is not null)
      .Select(MfgSite)
      .ToList();
    var plantSites = ids.Sites.Values
      .Where(s => s.Hub.Role == SystemRole.Industrial && s.MfgFacility is not null)
      .Select(MfgSite)
      .ToList();
    // Final retail shelves only (Station Sales) — consumption sink sites.
    var retailSites = ids.Sites.Values
      .Where(s => s.Facility is not null
                  && s.Hub.Role is SystemRole.Capital or SystemRole.Inhabited or SystemRole.Mining)
      .Select(Site)
      .ToList();
    var bunkerSites = ids.Sites.Values
      .Where(s => s.Hub.Role is SystemRole.Transit or SystemRole.Capital
        or SystemRole.Industrial or SystemRole.Mining)
      .Select(s => new AgentSite(
        s.Hub.LocationId, s.Facility ?? s.MfgFacility ?? s.CarrierPost, s.Hub.HubId, s.Hub.Name))
      .ToList();
    var allSites = ids.Sites.Values
      .Select(s => new AgentSite(
        s.Hub.LocationId, s.Facility ?? s.MfgFacility ?? s.CarrierPost, s.Hub.HubId, s.Hub.Name))
      .ToList();

    var mining = new ExtractiveFirmAgent(ids.Mining, new ExtractiveFirmAgentPolicy(
      miningSites, ids.Ore, ids.Parts,
      BaseOutputRate: 3.5m, OutputCap: PolityWorld.MineOreCap,
      InputPerOutput: PolityWorld.PartsPerOre, InputFloor: PolityWorld.MinePartsFloor,
      SellAboveStock: 8m, SellKeepFloor: 4m, SellMaxQty: 30m,
      OutputGatePrice: PolityWorld.OreBuy, InputLimitPrice: PolityWorld.PartsDelivered));

    var industry = new ManufacturingFirmAgent(ids.Industry, new ManufacturingFirmAgentPolicy(
      plantSites, ids.Ore, PolityWorld.PlantOreFloor + 12m, PolityWorld.OreDelivered,
      [
        // Capital intermediates keep mines alive (not a household sink).
        new ManufacturedSkuPolicy(
          ids.Parts, BaseRate: 6m, StockTarget: 55m, MinInputOnHand: 1m, RequiredInput: ids.Ore,
          SellAboveStock: 3m, SellKeepFloor: 2m, SellMaxQty: 24m, GatePrice: PolityWorld.PartsBuy),
        // Final — freight to Station shelves; household retail is the sink.
        new ManufacturedSkuPolicy(
          ids.Goods, BaseRate: 5.5m, StockTarget: PolityWorld.RetailStockTarget,
          MinInputOnHand: 1m, RequiredInput: ids.Parts,
          SellAboveStock: 2m, SellKeepFloor: 1m, SellMaxQty: 28m, GatePrice: PolityWorld.GoodsFactory),
        new ManufacturedSkuPolicy(
          ids.Fuel, BaseRate: 4m, StockTarget: 72m, MinInputOnHand: 6m, RequiredInput: ids.Ore,
          SellAboveStock: 10m, SellKeepFloor: 4m, SellMaxQty: 24m, GatePrice: PolityWorld.FuelUnitCost),
      ]));

    var station = new RetailFirmAgent(ids.Station, new RetailFirmAgentPolicy(
      retailSites, bunkerSites,
      [
        // Only Final on the consumer shelf — the closed-loop sink.
        new RetailSkuPolicy(
          ids.Goods, PolityWorld.GoodsSell, PolityWorld.RetailStockTarget,
          PolityWorld.GoodsDelivered, PostRetailPrice: true),
      ],
      new BunkerSkuPolicy(
        ids.Fuel, MinStock: 28m, BuyLimitPrice: PolityWorld.FuelUnitCost * 1.25m,
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
          FuelBuyLimitPrice: PolityWorld.FuelUnitCost * 1.5m,
          MinBunkerFuel: 8m),
        homeHubs[i],
        rngSalt: 0x43415252UL ^ (ulong)(i + 1) * 0x9E3779B97F4A7C15UL));
    }

    var treasury = new TreasuryFirmAgent(ids.Station, new TreasuryFirmAgentPolicy(
      [ids.Mining, ids.Industry, .. ids.Carriers],
      CashFloorToLend: 4_000m,
      BorrowerCashFloor: PolityWorld.FirmCashFloor + 400m,
      LoanPrincipal: Money.From(2_000m),
      AnnualInterestRate: 0.06m,
      TermHours: SimulationHour.HoursPerDay * 90,
      MaxActiveLoansToBorrower: 4));

    sim.Enqueue(new OriginateLoan(
      ids.Station, ids.Industry, Money.From(3_000m), 0.06m, SimulationHour.HoursPerDay * 150));

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
