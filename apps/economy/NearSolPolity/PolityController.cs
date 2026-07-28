using Novolis.Economy;
using Novolis.Economy.Logistics;
using Novolis.Economy.Production;
using Novolis.Economy.Simulation;

namespace NearSolPolity;

/// <summary>
/// Circular polity: mines need parts → ore; industry turns ore into parts/goods/fuel;
/// feeders move ore plant-ward and parts mine-ward.
/// </summary>
internal sealed class PolityController
{
  private readonly EconomySimulation _sim;
  private readonly PolityWorld.Ids _ids;
  private int _feederCooldown;

  public PolityController(EconomySimulation sim, PolityWorld.Ids ids)
  {
    _sim = sim;
    _ids = ids;
  }

  public string LastAction { get; private set; } = "polity idle";

  public void Tick()
  {
    var world = _sim.State.World;
    EnsureProductionPlans(world);
    EnsureRetailPrices();
    TopUpTransitFuel(world);
    TryCircularFeeder(world);
  }

  private decimal PolityCash(EconomyWorld world) =>
    world.Ledgers.TryGetValue(_ids.Polity, out var ledger) ? ledger.Cash.Amount : 0m;

  private decimal Qty(EconomyWorld world, InventoryLocationId loc, ProductId p) =>
    world.Inventory.GetQuantity(new InventoryKey(_ids.Polity, loc, p)).Value;

  private void EnsureProductionPlans(EconomyWorld world)
  {
    foreach (var site in _ids.Sites.Values)
    {
      if (site.PolityFacility is not { } facility)
      {
        continue;
      }

      switch (site.Hub.Role)
      {
        case SystemRole.Mining:
        {
          var ore = Qty(world, site.Hub.LocationId, _ids.Ore);
          var parts = Qty(world, site.Hub.LocationId, _ids.Parts);
          // Need maintenance parts; stop when ore piles up or parts exhausted.
          var rate = ore >= PolityWorld.MineOreCap || parts < PolityWorld.PartsPerOre
            ? 0m
            : 2m;
          _sim.Enqueue(new SetProductionPlan(_ids.Polity, facility, _ids.Ore, Quantity.From(rate)));
          if (rate == 0m)
          {
            LastAction = parts < PolityWorld.PartsPerOre
              ? $"mine starved (parts) @ {site.Hub.Name}"
              : $"mine idle (cap) @ {site.Hub.Name}";
          }

          break;
        }
        case SystemRole.Industrial:
        {
          var ore = Qty(world, site.Hub.LocationId, _ids.Ore);
          var parts = Qty(world, site.Hub.LocationId, _ids.Parts);
          // Prefer parts (feeds mines + goods); refine fuel when ore is ample.
          _sim.Enqueue(new SetProductionPlan(
            _ids.Polity, facility, _ids.Parts, Quantity.From(ore >= 1m ? 4m : 0m)));
          _sim.Enqueue(new SetProductionPlan(
            _ids.Polity, facility, _ids.Goods, Quantity.From(parts >= 1m ? 2m : 0m)));
          _sim.Enqueue(new SetProductionPlan(
            _ids.Polity, facility, _ids.Fuel, Quantity.From(ore >= 10m ? 2m : 0m)));
          break;
        }
      }
    }
  }

  private void EnsureRetailPrices()
  {
    foreach (var site in _ids.Sites.Values)
    {
      if (site.PolityFacility is not { } facility)
      {
        continue;
      }

      switch (site.Hub.Role)
      {
        case SystemRole.Capital:
        case SystemRole.Inhabited:
        case SystemRole.Industrial:
          _sim.Enqueue(new SetRetailPrice(_ids.Polity, facility, _ids.Goods, Money.From(PolityWorld.GoodsSell)));
          _sim.Enqueue(new SetRetailPrice(_ids.Polity, facility, _ids.Parts, Money.From(PolityWorld.PartsSell)));
          break;
        case SystemRole.Mining:
          // Spare parts for local upkeep / tramp lifts.
          _sim.Enqueue(new SetRetailPrice(_ids.Polity, facility, _ids.Ore, Money.From(PolityWorld.OreBuy)));
          break;
      }
    }
  }

  private void TopUpTransitFuel(EconomyWorld world)
  {
    foreach (var site in _ids.Sites.Values)
    {
      if (site.Hub.Role is not (SystemRole.Transit or SystemRole.Capital or SystemRole.Industrial or SystemRole.Mining))
      {
        continue;
      }

      var qty = Qty(world, site.Hub.LocationId, _ids.Fuel);
      var min = site.Hub.Role switch
      {
        SystemRole.Capital => 16m,
        SystemRole.Industrial => 12m,
        SystemRole.Transit => 14m,
        _ => 8m,
      };
      if (qty + 0.01m >= min)
      {
        continue;
      }

      // Prefer moving refined fuel from industrial stock before importing.
      var plant = _ids.Sites.Values
        .Where(s => s.Hub.Role == SystemRole.Industrial)
        .Select(s => (Site: s, Fuel: Qty(world, s.Hub.LocationId, _ids.Fuel)))
        .OrderByDescending(x => x.Fuel)
        .FirstOrDefault();

      if (plant.Site is not null && plant.Fuel >= 10m && PolityShipmentsBusy(world) is false)
      {
        // Local transfer via feeder path when possible; else import.
        if (TryEnqueueFeeder(world, plant.Site, site, _ids.Fuel, Math.Min(12m, plant.Fuel - 4m)))
        {
          LastAction = $"fuel feeder {plant.Site.Hub.Name}→{site.Hub.Name}";
          return;
        }
      }

      var cash = PolityCash(world);
      var critical = qty < 3m;
      if (!critical && cash < PolityWorld.PolityCashFloor)
      {
        LastAction = "fuel top-up skipped (cash floor)";
        continue;
      }

      if (cash < PolityWorld.FuelUnitCost * 5m)
      {
        LastAction = "fuel import blocked (no cash)";
        continue;
      }

      _sim.Enqueue(new PlaceProcurementOrder(
        _ids.Polity, site.Hub.LocationId, _ids.Fuel, Quantity.From(10m), Money.From(PolityWorld.FuelUnitCost)));
      LastAction = $"import fuel @ {site.Hub.Name}";
      return;
    }
  }

  private bool PolityShipmentsBusy(EconomyWorld world) =>
    world.Shipments.Any(s =>
      !s.IsLegacy && s.FirmId.Equals(_ids.Polity) && s.Status == ShipmentStatus.InTransit)
    || world.PendingPlanShipments.Any(p => p.FirmId.Equals(_ids.Polity));

  private void TryCircularFeeder(EconomyWorld world)
  {
    if (_feederCooldown > 0)
    {
      _feederCooldown--;
      return;
    }

    if (PolityShipmentsBusy(world))
    {
      return;
    }

    var mines = _ids.Sites.Values.Where(s => s.Hub.Role == SystemRole.Mining).ToList();
    var plants = _ids.Sites.Values.Where(s => s.Hub.Role == SystemRole.Industrial).ToList();
    if (mines.Count == 0 || plants.Count == 0)
    {
      return;
    }

    // 1) Parts plant → mine (maintenance) — highest priority for circularity.
    var needyMine = mines
      .Select(s => (Site: s, Parts: Qty(world, s.Hub.LocationId, _ids.Parts)))
      .Where(x => x.Parts < PolityWorld.MinePartsFloor)
      .OrderBy(x => x.Parts)
      .FirstOrDefault();
    var richPlant = plants
      .Select(s => (Site: s, Parts: Qty(world, s.Hub.LocationId, _ids.Parts)))
      .Where(x => x.Parts >= 15m)
      .OrderByDescending(x => x.Parts)
      .FirstOrDefault();

    if (needyMine.Site is not null && richPlant.Site is not null)
    {
      var qty = Math.Min(20m, richPlant.Parts - 5m);
      if (TryEnqueueFeeder(world, richPlant.Site, needyMine.Site, _ids.Parts, qty))
      {
        LastAction = $"feeder parts ×{qty:0} {richPlant.Site.Hub.Name}→{needyMine.Site.Hub.Name}";
        return;
      }
    }

    // 2) Ore mine → plant (feed refining).
    var oreMine = mines
      .Select(s => (Site: s, Ore: Qty(world, s.Hub.LocationId, _ids.Ore)))
      .Where(x => x.Ore >= 15m)
      .OrderByDescending(x => x.Ore)
      .FirstOrDefault();
    var orePlant = plants
      .Select(s => (Site: s, Ore: Qty(world, s.Hub.LocationId, _ids.Ore)))
      .OrderBy(x => x.Ore)
      .FirstOrDefault();

    if (oreMine.Site is not null && orePlant.Site is not null && orePlant.Ore < PolityWorld.PlantOreFloor)
    {
      var qty = Math.Min(20m, oreMine.Ore);
      if (TryEnqueueFeeder(world, oreMine.Site, orePlant.Site, _ids.Ore, qty))
      {
        LastAction = $"feeder ore ×{qty:0} {oreMine.Site.Hub.Name}→{orePlant.Site.Hub.Name}";
        return;
      }
    }

    // 3) Goods plant → capital/inhabited retail shelves.
    var goodsPlant = plants
      .Select(s => (Site: s, Goods: Qty(world, s.Hub.LocationId, _ids.Goods)))
      .Where(x => x.Goods >= 12m)
      .OrderByDescending(x => x.Goods)
      .FirstOrDefault();
    var retail = _ids.Sites.Values
      .Where(s => s.Hub.Role is SystemRole.Capital or SystemRole.Inhabited)
      .Select(s => (Site: s, Goods: Qty(world, s.Hub.LocationId, _ids.Goods)))
      .OrderBy(x => x.Goods)
      .FirstOrDefault();

    if (goodsPlant.Site is not null && retail.Site is not null && retail.Goods < 8m)
    {
      var qty = Math.Min(15m, goodsPlant.Goods - 4m);
      if (TryEnqueueFeeder(world, goodsPlant.Site, retail.Site, _ids.Goods, qty))
      {
        LastAction = $"feeder goods ×{qty:0} {goodsPlant.Site.Hub.Name}→{retail.Site.Hub.Name}";
      }
    }
  }

  private bool TryEnqueueFeeder(
    EconomyWorld world,
    PolityWorld.Site from,
    PolityWorld.Site to,
    ProductId product,
    decimal qty)
  {
    if (qty < 1m)
    {
      return false;
    }

    if (!ItineraryPlanner.TryPlan(
          from.Hub.HubId,
          to.Hub.HubId,
          Quantity.From(qty),
          _ids.Hull,
          world.Corridors,
          out _))
    {
      LastAction = $"feeder blocked {from.Hub.Name}→{to.Hub.Name}";
      return false;
    }

    // Need fuel at origin for bunkering.
    if (Qty(world, from.Hub.LocationId, _ids.Fuel) < 2m)
    {
      LastAction = $"feeder wait fuel @ {from.Hub.Name}";
      return false;
    }

    _sim.Enqueue(new PlanShipment(
      _ids.Polity,
      from.Hub.HubId.Value,
      to.Hub.HubId.Value,
      product,
      Quantity.From(qty),
      _ids.HullId.Value));
    _feederCooldown = 8;
    return true;
  }
}
