using Novolis.Economy;
using Novolis.Economy.Logistics;
using Novolis.Economy.Production;
using Novolis.Economy.Simulation;

namespace NearSolPolity;

internal sealed record FleetJobQuote(
  string Name,
  TransportHubId Origin,
  TransportHubId Destination,
  InventoryLocationId BuyAt,
  InventoryLocationId SellAt,
  FacilityId SellFacility,
  ProductId Product,
  decimal Quantity,
  decimal BuyUnit,
  decimal SellUnit,
  long UnderwayHours,
  decimal FuelUnits,
  decimal Tolls,
  decimal CrewHours,
  decimal WageRate,
  decimal FuelUnitCost,
  bool Feasible)
{
  public decimal Revenue => Quantity * SellUnit;
  public decimal Cog => Quantity * BuyUnit;
  public decimal FuelCost => FuelUnits * FuelUnitCost;
  public decimal CrewCost => CrewHours * WageRate;
  public decimal Margin => Feasible
    ? Revenue - Cog - FuelCost - Tolls - CrewCost
    : decimal.MinValue / 4m;

  public string Summary =>
    Feasible
      ? $"{Name} Δ{Margin:0.#} (rev {Revenue:0} fuel {FuelCost:0} toll {Tolls:0} crew {CrewCost:0})"
      : $"{Name} infeasible";
}

/// <summary>Graph-aware tramp autopilot for the near-Sol polity.</summary>
internal sealed class TrampFleetAutopilot
{
  private readonly EconomySimulation _sim;
  private readonly PolityWorld.Ids _ids;
  private TransportHubId _currentHub;
  private int _sellWait;
  private bool _askPosted;
  private ProductId? _selling;
  private FacilityId? _sellFacility;
  private InventoryLocationId? _sellLoc;

  public TrampFleetAutopilot(EconomySimulation sim, PolityWorld.Ids ids)
  {
    _sim = sim;
    _ids = ids;
    _currentHub = ids.Sites["sol"].Hub.HubId;
  }

  public string LastDecision { get; private set; } = "standing by";
  public string LastEval { get; private set; } = "";
  public TransportHubId CurrentHub => _currentHub;

  public void Tick()
  {
    var world = _sim.State.World;
    var ship = world.Shipments.FirstOrDefault(s =>
      !s.IsLegacy && s.FirmId.Equals(_ids.Tramp) && s.Status == ShipmentStatus.InTransit);

    if (ship is not null)
    {
      _currentHub = ship.CurrentHubId;
      LastDecision = $"underway @ {HubName(_currentHub)}";
      return;
    }

    if (world.PendingPlanShipments.Any(p => p.FirmId.Equals(_ids.Tramp))
        || world.PendingProcurement.Any(p => p.BuyerFirmId.Equals(_ids.Tramp)))
    {
      LastDecision = "pending tramp orders";
      return;
    }

    // Snap current hub from delivered cargo presence if needed.
    InferHubFromCargo(world);
    TopUpFuelNearCurrent();

    // Ore / parts delivered for industrial inputs settle B2B into polity stock.
    if (TrySettleToPolity(world))
    {
      return;
    }

    if (_askPosted && _selling is { } product && _sellLoc is { } loc && _sellFacility is { })
    {
      var remaining = Qty(_ids.Tramp, loc, product);
      if (remaining > 0.5m && _sellWait < 48)
      {
        _sellWait++;
        LastDecision = $"waiting sales ({ProductLabel(product)} {remaining:0})";
        return;
      }

      _askPosted = false;
      _selling = null;
      _sellFacility = null;
      _sellLoc = null;
      _sellWait = 0;
    }

    // Sell delivered tramp parts/goods (consumer demand) before accepting a new haul.
    foreach (var site in _ids.Sites.Values.Where(s => s.TrampPost is not null))
    {
      foreach (var (sku, price) in ConsumerSellBook())
      {
        var have = Qty(_ids.Tramp, site.Hub.LocationId, sku);
        if (have < 1m)
        {
          continue;
        }

        _sim.Enqueue(new SetRetailPrice(_ids.Tramp, site.TrampPost!.Value, sku, Money.From(price)));
        _askPosted = true;
        _selling = sku;
        _sellFacility = site.TrampPost;
        _sellLoc = site.Hub.LocationId;
        _sellWait = 0;
        _currentHub = site.Hub.HubId;
        LastDecision = $"list {ProductLabel(sku)} @ {site.Hub.Name} {price:0}";
        LastEval = "delivered cargo — sell before next accept";
        return;
      }
    }

    var quotes = BuildQuotes(world)
      .OrderByDescending(q => q.Margin)
      .ToList();
    LastEval = string.Join(" · ", quotes.Take(3).Select(q =>
      q.Feasible ? $"{q.Name} Δ{q.Margin:0}" : $"{q.Name} no"));

    var best = quotes.FirstOrDefault(q => q.Feasible && q.Margin >= PolityWorld.MinMargin);
    if (best is null)
    {
      var near = quotes.FirstOrDefault(q => q.Feasible);
      LastDecision = near is null
        ? "idle — no feasible jobs"
        : $"idle — best {near.Name} Δ{near.Margin:0.#} < min {PolityWorld.MinMargin}";
      return;
    }

    CommitJob(best);
  }

  private IEnumerable<(ProductId Product, decimal Price)> ConsumerSellBook()
  {
    yield return (_ids.Parts, PolityWorld.PartsSell);
    yield return (_ids.Goods, PolityWorld.GoodsSell);
  }

  /// <summary>
  /// Moves tramp industrial inputs into polity inventory and pays freight.
  /// Ore → industrial/capital; parts → mining (maintenance).
  /// </summary>
  private bool TrySettleToPolity(EconomyWorld world)
  {
    // Ore at refining hubs.
    foreach (var site in _ids.Sites.Values
               .Where(s => s.Hub.Role is SystemRole.Industrial or SystemRole.Capital))
    {
      if (TrySettleProduct(world, site, _ids.Ore, PolityWorld.OreFreightUnit))
      {
        return true;
      }
    }

    // Parts at mines (maintenance deliveries).
    foreach (var site in _ids.Sites.Values.Where(s => s.Hub.Role == SystemRole.Mining))
    {
      if (TrySettleProduct(world, site, _ids.Parts, PolityWorld.PartsFreightUnit))
      {
        return true;
      }
    }

    return false;
  }

  private bool TrySettleProduct(
    EconomyWorld world,
    PolityWorld.Site site,
    ProductId product,
    decimal unitPrice)
  {
    var have = Qty(_ids.Tramp, site.Hub.LocationId, product);
    if (have < 1m)
    {
      return false;
    }

    var revenue = Money.From(have * unitPrice);
    var polityCash = world.Ledgers.TryGetValue(_ids.Polity, out var peek) ? peek.Cash.Amount : 0m;
    // Soft floor: still settle if polity can cover the invoice without going negative.
    if (polityCash < revenue.Amount)
    {
      LastDecision = $"hold {ProductLabel(product)} ×{have:0} @ {site.Hub.Name} (polity cash)";
      LastEval = "B2B deferred — co-op short cash";
      _currentHub = site.Hub.HubId;
      return true;
    }

    _sim.Enqueue(new TransferGoodsForCash(
      _ids.Tramp,
      _ids.Polity,
      site.Hub.LocationId,
      product,
      Quantity.From(have),
      Money.From(unitPrice)));

    _currentHub = site.Hub.HubId;
    LastDecision = $"settle {ProductLabel(product)} ×{have:0} → polity @ {site.Hub.Name}";
    LastEval = $"B2B freight @ {unitPrice:0}/u";
    return true;
  }

  private List<FleetJobQuote> BuildQuotes(EconomyWorld world)
  {
    var wage = world.Policy.WageRatePerHour.Amount;
    var fuelCost = world.TransportFuelUnitCost.Amount;
    var quotes = new List<FleetJobQuote>();

    var mines = _ids.Sites.Values.Where(s => s.Hub.Role == SystemRole.Mining).ToList();
    var sinks = _ids.Sites.Values
      .Where(s => s.Hub.Role is SystemRole.Industrial or SystemRole.Capital)
      .Where(s => s.TrampPost is not null)
      .ToList();
    var capitals = _ids.Sites.Values
      .Where(s => s.Hub.Role is SystemRole.Capital or SystemRole.Inhabited)
      .Where(s => s.TrampPost is not null)
      .ToList();
    var industrials = _ids.Sites.Values.Where(s => s.Hub.Role == SystemRole.Industrial).ToList();

    // Ore: mine → industrial / capital
    foreach (var mine in mines.Take(6))
    {
      var stock = Qty(_ids.Polity, mine.Hub.LocationId, _ids.Ore)
                  + Qty(_ids.Tramp, mine.Hub.LocationId, _ids.Ore);
      if (stock < 5m)
      {
        continue;
      }

      foreach (var sink in sinks.Take(4))
      {
        if (sink.Hub.SystemId.Equals(mine.Hub.SystemId, StringComparison.OrdinalIgnoreCase))
        {
          continue;
        }

        quotes.Add(Quote(
          world, $"Ore {Short(mine.Hub.Name)}→{Short(sink.Hub.Name)}",
          mine.Hub, sink.Hub, sink.TrampPost!.Value,
          _ids.Ore, Math.Min(20m, Math.Max(10m, stock)),
          PolityWorld.OreBuy, PolityWorld.OreFreightUnit, wage, fuelCost));
      }
    }

    // Parts: industrial → mining (maintenance) — closes the production circle.
    foreach (var plant in industrials)
    {
      var stock = Qty(_ids.Polity, plant.Hub.LocationId, _ids.Parts)
                  + Qty(_ids.Tramp, plant.Hub.LocationId, _ids.Parts);
      if (stock < 4m)
      {
        continue;
      }

      foreach (var mine in mines.Take(6))
      {
        if (mine.TrampPost is not { } minePost)
        {
          continue;
        }

        var mineParts = Qty(_ids.Polity, mine.Hub.LocationId, _ids.Parts);
        if (mineParts >= PolityWorld.MinePartsFloor * 2m)
        {
          continue;
        }

        quotes.Add(Quote(
          world, $"Parts {Short(plant.Hub.Name)}→{Short(mine.Hub.Name)}",
          plant.Hub, mine.Hub, minePost,
          _ids.Parts, Math.Min(15m, Math.Max(8m, stock)),
          PolityWorld.PartsBuy, PolityWorld.PartsFreightUnit, wage, fuelCost));
      }

      foreach (var dest in capitals.Take(4))
      {
        if (dest.Hub.SystemId.Equals(plant.Hub.SystemId, StringComparison.OrdinalIgnoreCase))
        {
          continue;
        }

        quotes.Add(Quote(
          world, $"Parts {Short(plant.Hub.Name)}→{Short(dest.Hub.Name)}",
          plant.Hub, dest.Hub, dest.TrampPost!.Value,
          _ids.Parts, Math.Min(12m, Math.Max(6m, stock)),
          PolityWorld.PartsBuy, PolityWorld.PartsSell, wage, fuelCost));
      }
    }

    // Goods: industrial / capital → inhabited
    var goodsOrigins = _ids.Sites.Values
      .Where(s => s.Hub.Role is SystemRole.Industrial or SystemRole.Capital)
      .ToList();
    var inhabited = _ids.Sites.Values
      .Where(s => s.Hub.Role == SystemRole.Inhabited && s.TrampPost is not null)
      .Take(6)
      .ToList();

    foreach (var origin in goodsOrigins.Take(4))
    {
      var stock = Qty(_ids.Polity, origin.Hub.LocationId, _ids.Goods)
                  + Qty(_ids.Tramp, origin.Hub.LocationId, _ids.Goods);
      if (stock < 4m)
      {
        continue;
      }

      foreach (var dest in inhabited)
      {
        quotes.Add(Quote(
          world, $"Goods {Short(origin.Hub.Name)}→{Short(dest.Hub.Name)}",
          origin.Hub, dest.Hub, dest.TrampPost!.Value,
          _ids.Goods, Math.Min(12m, Math.Max(6m, stock)),
          PolityWorld.PartsSell, PolityWorld.GoodsSell, wage, fuelCost));
      }
    }

    return quotes;
  }

  private FleetJobQuote Quote(
    EconomyWorld world,
    string name,
    AstroEconomyBridge.HubBinding origin,
    AstroEconomyBridge.HubBinding dest,
    FacilityId sellFacility,
    ProductId product,
    decimal qty,
    decimal buyUnit,
    decimal sellUnit,
    decimal wage,
    decimal fuelCost)
  {
    qty = Math.Min(qty, _ids.Hull.CargoCapacity.Value);
    if (!ItineraryPlanner.TryPlan(
          origin.HubId, dest.HubId, Quantity.From(qty), _ids.Hull, world.Corridors, out var itinerary))
    {
      return new FleetJobQuote(
        name, origin.HubId, dest.HubId, origin.LocationId, dest.LocationId, sellFacility,
        product, qty, buyUnit, sellUnit, 0, 0, 0, 0, wage, fuelCost, false);
    }

    long hours = 0;
    decimal fuel = 0;
    decimal tolls = 0;
    foreach (var legId in itinerary.CorridorIds)
    {
      var leg = world.Corridors[legId];
      hours += Math.Max(1, leg.TransitHours);
      fuel += leg.TransitHours * leg.Difficulty * _ids.Hull.FuelBurnPerDifficultyHour;
      tolls += leg.Toll.Amount;
    }

    var crewHours = hours * _ids.Hull.CrewLaborPerUnderwayHour;
    return new FleetJobQuote(
      name, origin.HubId, dest.HubId, origin.LocationId, dest.LocationId, sellFacility,
      product, qty, buyUnit, sellUnit, hours, fuel, tolls, crewHours, wage, fuelCost, true);
  }

  private void CommitJob(FleetJobQuote job)
  {
    var have = Qty(_ids.Tramp, job.BuyAt, job.Product);
    var need = job.Quantity - have;
    if (need > 0.01m)
    {
      // Closed loop: lift from co-op stock. No exogenous cargo mint.
      if (TryBuyFromPolity(job.BuyAt, job.Product, need, job.BuyUnit))
      {
        LastDecision = $"lift {ProductLabel(job.Product)} ×{need:0} from co-op for {job.Name}";
        LastEval = job.Summary;
        return;
      }

      LastDecision = $"idle — no co-op stock for {job.Name}";
      LastEval = job.Summary;
      return;
    }

    _sim.Enqueue(new PlanShipment(
      _ids.Tramp,
      job.Origin.Value,
      job.Destination.Value,
      job.Product,
      Quantity.From(job.Quantity),
      _ids.HullId.Value));
    LastDecision = $"accept {job.Name} qty {job.Quantity:0} Δ{job.Margin:0.#}";
    LastEval = job.Summary;
  }

  /// <summary>Transfers cargo from polity → tramp at unit price (tramp pays, co-op earns).</summary>
  private bool TryBuyFromPolity(InventoryLocationId loc, ProductId product, decimal need, decimal unitPrice)
  {
    var world = _sim.State.World;
    var polityHave = Qty(_ids.Polity, loc, product);
    if (polityHave < need)
    {
      return false;
    }

    var trampCash = world.Ledgers.TryGetValue(_ids.Tramp, out var tPeek) ? tPeek.Cash.Amount : 0m;
    var cost = Money.From(need * unitPrice);
    if (trampCash < cost.Amount)
    {
      return false;
    }

    _sim.Enqueue(new TransferGoodsForCash(
      _ids.Polity,
      _ids.Tramp,
      loc,
      product,
      Quantity.From(need),
      Money.From(unitPrice)));
    return true;
  }

  private void TopUpFuelNearCurrent()
  {
    var site = _ids.Sites.Values.FirstOrDefault(s => s.Hub.HubId.Equals(_currentHub))
               ?? _ids.Sites["sol"];
    var fuel = Qty(_ids.Tramp, site.Hub.LocationId, _ids.Fuel);
    if (fuel >= 8m)
    {
      return;
    }

    var need = 12m;
    // Prefer co-op bunker stock (closed loop). Exogenous fuel is an import leak.
    if (TryBuyFromPolity(site.Hub.LocationId, _ids.Fuel, need, PolityWorld.FuelUnitCost))
    {
      return;
    }

    _sim.Enqueue(new PlaceProcurementOrder(
      _ids.Tramp, site.Hub.LocationId, _ids.Fuel, Quantity.From(need), Money.From(PolityWorld.FuelUnitCost)));
  }

  private void InferHubFromCargo(EconomyWorld world)
  {
    foreach (var site in _ids.Sites.Values)
    {
      var ore = Qty(_ids.Tramp, site.Hub.LocationId, _ids.Ore);
      var parts = Qty(_ids.Tramp, site.Hub.LocationId, _ids.Parts);
      var goods = Qty(_ids.Tramp, site.Hub.LocationId, _ids.Goods);
      if (ore + parts + goods >= 1m)
      {
        _currentHub = site.Hub.HubId;
        return;
      }
    }
  }

  private decimal Qty(FirmId firm, InventoryLocationId loc, ProductId p) =>
    _sim.State.World.Inventory.GetQuantity(new InventoryKey(firm, loc, p)).Value;

  private string HubName(TransportHubId id) =>
    _sim.State.World.Hubs.TryGetValue(id, out var h) ? h.Name : "?";

  private string ProductLabel(ProductId p)
  {
    if (p.Equals(_ids.Ore)) return "ore";
    if (p.Equals(_ids.Parts)) return "parts";
    if (p.Equals(_ids.Goods)) return "goods";
    return "cargo";
  }

  private static string Short(string name) =>
    name.Length <= 10 ? name : name[..9] + "…";
}
