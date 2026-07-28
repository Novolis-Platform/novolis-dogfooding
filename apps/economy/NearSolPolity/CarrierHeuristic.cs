using Novolis.Economy;
using Novolis.Economy.Logistics;
using Novolis.Economy.Markets;
using Novolis.Economy.Production;
using Novolis.Economy.Simulation;

namespace NearSolPolity;

/// <summary>Carrier: clear cross-hub sell@A + buy@B spreads via haul (heuristics + RNG ties).</summary>
internal sealed class CarrierHeuristic
{
  private readonly EconomySimulation _sim;
  private readonly PolityWorld.Ids _ids;
  private readonly DeterministicRandom _rng;
  private readonly Dictionary<InventoryLocationId, PolityWorld.Site> _siteByLoc;
  private readonly Dictionary<(Guid Origin, Guid Dest), Itinerary?> _routeCache = new();
  private TransportHubId _currentHub;
  private SpreadJob? _activeHaul;

  public CarrierHeuristic(EconomySimulation sim, PolityWorld.Ids ids)
  {
    _sim = sim;
    _ids = ids;
    _rng = new DeterministicRandom(sim.State.Seed ^ 0x43415252UL);
    _currentHub = ids.Sites["sol"].Hub.HubId;
    _siteByLoc = ids.Sites.Values.ToDictionary(s => s.Hub.LocationId);
  }

  public string LastDecision { get; private set; } = "standing by";
  public string LastEval { get; private set; } = "";
  public TransportHubId CurrentHub => _currentHub;

  public void Tick()
  {
    var world = _sim.State.World;
    var ship = world.Shipments.FirstOrDefault(s =>
      !s.IsLegacy && s.FirmId.Equals(_ids.Carrier) && s.Status == ShipmentStatus.InTransit);
    if (ship is not null)
    {
      _currentHub = ship.CurrentHubId;
      LastDecision = $"underway @ {HubName(_currentHub)}";
      return;
    }

    if (world.PendingPlanShipments.Any(p => p.FirmId.Equals(_ids.Carrier)))
    {
      LastDecision = "awaiting departure";
      return;
    }

    // Prefer hauling owned freight to a paying bid over dumping at origin.
    // Energy on the hull is bunker fuel — never treat it as tradable cargo here.
    foreach (var site in _ids.Sites.Values)
    {
      foreach (var sku in new[] { _ids.Ore, _ids.Parts, _ids.Goods })
      {
        var have = Qty(_ids.Carrier, site.Hub.LocationId, sku);
        if (have < 1m)
        {
          continue;
        }

        _currentHub = site.Hub.HubId;
        if (_activeHaul is { } haul
            && haul.Product.Equals(sku)
            && haul.OriginLoc.Equals(site.Hub.LocationId)
            && have + 0.01m >= Math.Min(haul.Quantity, 1m))
        {
          if (!EnsureBunker(site))
          {
            LastDecision = $"bunkering @ {site.Hub.Name}";
            LastEval = haul.Summary;
            return;
          }

          var qty = Math.Min(have, haul.Quantity);
          _sim.Enqueue(new PlanShipment(
            _ids.Carrier, haul.OriginHub.Value, haul.DestHub.Value,
            sku, Quantity.From(qty), _ids.HullId.Value));
          LastDecision = $"haul {haul.Name} ×{qty:0}";
          LastEval = haul.Summary;
          return;
        }

        // Prefer clearing into a local bid over starting another hop.
        if (world.HubOrders.Any(o =>
              o.Side == HubOrderSide.Buy && !o.IsFilled
              && o.LocationId.Equals(site.Hub.LocationId)
              && o.ProductId.Equals(sku)
              && !o.FirmId.Equals(_ids.Carrier)
              && o.LimitPrice.Amount + 0.0001m >= Gate(sku)))
        {
          OfferLocalSale(world, site, sku, have);
          _activeHaul = null;
          return;
        }

        var outbound = BestOutboundFrom(world, site, sku, have);
        if (outbound is not null)
        {
          if (!EnsureBunker(site))
          {
            _activeHaul = outbound;
            LastDecision = $"bunkering @ {site.Hub.Name}";
            LastEval = outbound.Summary;
            return;
          }

          _activeHaul = outbound;
          var qty = Math.Min(have, outbound.Quantity);
          _sim.Enqueue(new PlanShipment(
            _ids.Carrier, outbound.OriginHub.Value, outbound.DestHub.Value,
            sku, Quantity.From(qty), _ids.HullId.Value));
          LastDecision = $"haul {outbound.Name} ×{qty:0}";
          LastEval = outbound.Summary;
          return;
        }

        OfferLocalSale(world, site, sku, have);
        _activeHaul = null;
        return;
      }
    }

    TopUpFuel();
    // Drop stale lift/bunker bids only — keep delivery offers on the book until filled.
    foreach (var order in world.HubOrders
               .Where(o => o.FirmId.Equals(_ids.Carrier) && !o.IsFilled && o.Side == HubOrderSide.Buy)
               .Select(o => o.Id)
               .ToList())
    {
      _sim.Enqueue(new CancelHubOrder(order));
    }

    var allJobs = BuildSpreadJobs(world);
    var candidates = allJobs
      .Where(j => j.Margin >= PolityWorld.MinMargin)
      .OrderByDescending(j => j.Margin)
      .ThenBy(_ => _rng.NextDouble())
      .ToList();

    LastEval = string.Join(" · ", candidates.Take(3).Select(c => $"{c.Name} Δ{c.Margin:0}"));
    var best = candidates.FirstOrDefault();
    if (best is null)
    {
      _activeHaul = null;
      LastDecision = "idle — no book spreads";
      if (allJobs.Count == 0)
      {
        LastEval = $"no pairs (open={world.HubOrders.Count})";
      }
      else
      {
        LastEval = "below min " + string.Join(" · ", allJobs.OrderByDescending(j => j.Margin).Take(3)
          .Select(c => $"{c.Name} Δ{c.Margin:0}"));
      }

      return;
    }

    _activeHaul = best;
    var originSite = _ids.Sites.Values.First(s => s.Hub.LocationId.Equals(best.OriginLoc));
    if (!EnsureBunker(originSite))
    {
      // Still post the lift buy so Match can stock cargo while we wait for bunker fills.
      _sim.Enqueue(new PostHubOrder(
        _ids.Carrier, best.OriginLoc, best.Product, HubOrderSide.Buy,
        Quantity.From(best.Quantity), Money.From(best.LiftLimit)));
      LastDecision = $"lift {best.Name} (await bunker)";
      LastEval = best.Summary;
      return;
    }

    // Lift + plan same hour: MatchHubOrders fills buy before AcquireInputs plans haul.
    _sim.Enqueue(new PostHubOrder(
      _ids.Carrier, best.OriginLoc, best.Product, HubOrderSide.Buy,
      Quantity.From(best.Quantity), Money.From(best.LiftLimit)));
    _sim.Enqueue(new PlanShipment(
      _ids.Carrier, best.OriginHub.Value, best.DestHub.Value,
      best.Product, Quantity.From(best.Quantity), _ids.HullId.Value));
    LastDecision = $"lift+haul {best.Name} qty {best.Quantity:0}";
    LastEval = best.Summary;
  }

  private SpreadJob? BestOutboundFrom(
    EconomyWorld world, PolityWorld.Site origin, ProductId sku, decimal have)
  {
    var wage = world.Policy.WageRatePerHour;
    var fuelCost = world.TransportFuelUnitCost;
    var buys = world.HubOrders
      .Where(b => b.Side == HubOrderSide.Buy && !b.IsFilled
                  && b.ProductId.Equals(sku)
                  && !b.LocationId.Equals(origin.Hub.LocationId)
                  && !b.FirmId.Equals(_ids.Carrier))
      .OrderByDescending(b => b.LimitPrice.Amount)
      .ThenBy(_ => _rng.NextDouble())
      .ToList();

    SpreadJob? best = null;
    foreach (var buy in buys.Take(25))
    {
      var dest = _siteByLoc.GetValueOrDefault(buy.LocationId);
      if (dest is null)
      {
        continue;
      }

      var qty = Math.Min(have, Math.Min(buy.Remaining.Value, _ids.Hull.CargoCapacity.Value));
      if (qty < 1m)
      {
        continue;
      }

      if (!TryGetRoute(origin.Hub.HubId, dest.Hub.HubId, world, out var itinerary))
      {
        continue;
      }

      var est = HaulCostEstimator.Estimate(itinerary, world.Corridors, _ids.Hull, wage, fuelCost);
      var cog = Gate(sku);
      var margin = qty * buy.LimitPrice.Amount - qty * cog - est.TotalVariableCost.Amount;
      if (margin < PolityWorld.MinMargin)
      {
        continue;
      }

      var job = new SpreadJob(
        $"{PolityWorld.SkuLabel(sku, _ids)} {Short(origin.Hub.Name)}→{Short(dest.Hub.Name)}",
        origin.Hub.HubId, dest.Hub.HubId, origin.Hub.LocationId, dest.Hub.LocationId,
        sku, qty, cog, buy.LimitPrice.Amount, margin,
        $"Δ{margin:0.#} haul {est.TotalVariableCost.Amount:0}");
      if (best is null || job.Margin > best.Margin)
      {
        best = job;
      }
    }

    return best;
  }

  private List<SpreadJob> BuildSpreadJobs(EconomyWorld world)
  {
    var wage = world.Policy.WageRatePerHour;
    var fuelCost = world.TransportFuelUnitCost;
    var sells = new List<HubOrder>(32);
    var buysByProduct = new Dictionary<ProductId, List<HubOrder>>();
    foreach (var o in world.HubOrders)
    {
      if (o.IsFilled || o.FirmId.Equals(_ids.Carrier))
      {
        continue;
      }

      if (o.Side == HubOrderSide.Sell)
      {
        if (sells.Count < 40)
        {
          sells.Add(o);
        }
      }
      else
      {
        if (!buysByProduct.TryGetValue(o.ProductId, out var list))
        {
          list = new List<HubOrder>(8);
          buysByProduct[o.ProductId] = list;
        }

        if (list.Count < 24)
        {
          list.Add(o);
        }
      }
    }

    var jobs = new List<SpreadJob>();
    foreach (var sell in sells)
    {
      if (!buysByProduct.TryGetValue(sell.ProductId, out var buys))
      {
        continue;
      }

      if (!_siteByLoc.TryGetValue(sell.LocationId, out var origin))
      {
        continue;
      }

      var matched = 0;
      foreach (var buy in buys)
      {
        if (matched >= 20)
        {
          break;
        }

        if (buy.LocationId.Equals(sell.LocationId)
            || buy.LimitPrice.Amount < sell.LimitPrice.Amount)
        {
          continue;
        }

        if (!_siteByLoc.TryGetValue(buy.LocationId, out var dest))
        {
          continue;
        }

        var qty = Math.Min(sell.Remaining.Value, buy.Remaining.Value);
        qty = Math.Min(qty, _ids.Hull.CargoCapacity.Value);
        if (qty < 2m)
        {
          continue;
        }

        if (!TryGetRoute(origin.Hub.HubId, dest.Hub.HubId, world, out var itinerary))
        {
          continue;
        }

        matched++;
        var est = HaulCostEstimator.Estimate(itinerary, world.Corridors, _ids.Hull, wage, fuelCost);
        var lift = Math.Min(buy.LimitPrice.Amount, sell.LimitPrice.Amount * 1.12m);
        var revenue = qty * buy.LimitPrice.Amount;
        var cog = qty * lift;
        var margin = revenue - cog - est.TotalVariableCost.Amount;
        jobs.Add(new SpreadJob(
          $"{PolityWorld.SkuLabel(sell.ProductId, _ids)} {Short(origin.Hub.Name)}→{Short(dest.Hub.Name)}",
          origin.Hub.HubId, dest.Hub.HubId, sell.LocationId, dest.Hub.LocationId,
          sell.ProductId, qty, lift, buy.LimitPrice.Amount, margin,
          $"Δ{margin:0.#} haul {est.TotalVariableCost.Amount:0}"));
      }
    }

    return jobs;
  }

  private bool TryGetRoute(
    TransportHubId origin,
    TransportHubId dest,
    EconomyWorld world,
    out Itinerary itinerary)
  {
    var key = (origin.Value, dest.Value);
    if (_routeCache.TryGetValue(key, out var cached))
    {
      if (cached is null)
      {
        itinerary = default!;
        return false;
      }

      itinerary = cached;
      return true;
    }

    if (!ItineraryPlanner.TryPlan(
          origin, dest, _ids.Hull.CargoCapacity, _ids.Hull, world.Corridors, out itinerary))
    {
      _routeCache[key] = null;
      return false;
    }

    _routeCache[key] = itinerary;
    return true;
  }

  private void OfferLocalSale(EconomyWorld world, PolityWorld.Site site, ProductId sku, decimal have)
  {
    BookQuotes.CancelOpen(_sim, _ids.Carrier, site.Hub.LocationId, sku);
    var bestBid = world.HubOrders
      .Where(o => o.Side == HubOrderSide.Buy && !o.IsFilled
                  && o.LocationId.Equals(site.Hub.LocationId)
                  && o.ProductId.Equals(sku)
                  && !o.FirmId.Equals(_ids.Carrier))
      .Select(o => o.LimitPrice.Amount)
      .DefaultIfEmpty(0m)
      .Max();
    var px = bestBid > 0m
      ? bestBid * (0.98m + (decimal)(_rng.NextDouble() * 0.02))
      : Gate(sku) * (1m + PolityWorld.FreightPremiumPerUnit / Math.Max(4m, Gate(sku)));
    _sim.Enqueue(new PostHubOrder(
      _ids.Carrier, site.Hub.LocationId, sku, HubOrderSide.Sell,
      Quantity.From(have), Money.From(Math.Round(px, 2))));
    LastDecision = $"offer {PolityWorld.SkuLabel(sku, _ids)} ×{have:0} @ {site.Hub.Name}";
    LastEval = bestBid > 0m ? $"clear bid {bestBid:0.##}" : "deliver into book";
  }

  private bool EnsureBunker(PolityWorld.Site site)
  {
    var fuel = Qty(_ids.Carrier, site.Hub.LocationId, _ids.Fuel);
    if (fuel >= 4m)
    {
      return true;
    }

    TopUpFuelAt(site);
    // Also pull from station bunkers via procurement when the book is dry.
    if (fuel < 2m)
    {
      _sim.Enqueue(new PlaceProcurementOrder(
        _ids.Carrier, site.Hub.LocationId, _ids.Fuel, Quantity.From(8m),
        Money.From(PolityWorld.FuelUnitCost * 1.5m)));
    }

    return Qty(_ids.Carrier, site.Hub.LocationId, _ids.Fuel) >= 4m;
  }

  private void TopUpFuel()
  {
    var site = _ids.Sites.Values.FirstOrDefault(s => s.Hub.HubId.Equals(_currentHub))
               ?? _ids.Sites["sol"];
    TopUpFuelAt(site);
  }

  private void TopUpFuelAt(PolityWorld.Site site)
  {
    var fuel = Qty(_ids.Carrier, site.Hub.LocationId, _ids.Fuel);
    if (fuel >= 6m)
    {
      return;
    }

    BookQuotes.CancelOpen(_sim, _ids.Carrier, site.Hub.LocationId, _ids.Fuel);
    _sim.Enqueue(new PostHubOrder(
      _ids.Carrier, site.Hub.LocationId, _ids.Fuel, HubOrderSide.Buy, Quantity.From(12m),
      Money.From(PolityWorld.FuelUnitCost * 1.25m)));
  }

  private decimal Gate(ProductId p)
  {
    if (p.Equals(_ids.Ore)) return PolityWorld.OreBuy;
    if (p.Equals(_ids.Parts)) return PolityWorld.PartsBuy;
    if (p.Equals(_ids.Goods)) return PolityWorld.GoodsSell * 0.55m;
    return PolityWorld.FuelUnitCost;
  }

  private decimal Qty(FirmId firm, InventoryLocationId loc, ProductId p) =>
    _sim.State.World.Inventory.GetQuantity(new InventoryKey(firm, loc, p)).Value;

  private string HubName(TransportHubId id) =>
    _sim.State.World.Hubs.TryGetValue(id, out var h) ? h.Name : "?";

  private static string Short(string name) =>
    name.Length <= 14 ? name : name[..12] + "…";

  private sealed record SpreadJob(
    string Name,
    TransportHubId OriginHub,
    TransportHubId DestHub,
    InventoryLocationId OriginLoc,
    InventoryLocationId DestLoc,
    ProductId Product,
    decimal Quantity,
    decimal LiftLimit,
    decimal DestBid,
    decimal Margin,
    string Summary);
}
