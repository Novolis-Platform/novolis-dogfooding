using Novolis.Economy;
using Novolis.Economy.Markets;
using Novolis.Economy.Production;
using Novolis.Economy.Simulation;

namespace NearSolPolity;

/// <summary>Station firm: buy Final on book, post retail, own toll treasury + bunkers.</summary>
internal sealed class StationHeuristic
{
  private readonly EconomySimulation _sim;
  private readonly PolityWorld.Ids _ids;
  private readonly DeterministicRandom _rng;

  public StationHeuristic(EconomySimulation sim, PolityWorld.Ids ids)
  {
    _sim = sim;
    _ids = ids;
    _rng = new DeterministicRandom(sim.State.Seed ^ 0x5354414EUL);
  }

  public string LastAction { get; private set; } = "station idle";

  public void Tick()
  {
    BookQuotes.CancelOpen(_sim, _ids.Station);
    var world = _sim.State.World;
    foreach (var site in _ids.Sites.Values)
    {
      var loc = site.Hub.LocationId;
      if (site.Facility is { } facility
          && site.Hub.Role is SystemRole.Capital or SystemRole.Inhabited or SystemRole.Industrial)
      {
        var goodsStock = Qty(_ids.Station, loc, _ids.Goods);
        var partsStock = Qty(_ids.Station, loc, _ids.Parts);
        var goodsPrice = InventoryPressurePricing.Adjust(
          Money.From(PolityWorld.GoodsSell), goodsStock, PolityWorld.RetailStockTarget);
        var partsPrice = InventoryPressurePricing.Adjust(
          Money.From(PolityWorld.PartsSell), partsStock, PolityWorld.RetailStockTarget);
        _sim.Enqueue(new SetRetailPrice(_ids.Station, facility, _ids.Goods, goodsPrice));
        _sim.Enqueue(new SetRetailPrice(_ids.Station, facility, _ids.Parts, partsPrice));

        if (goodsStock < PolityWorld.RetailStockTarget)
        {
          var need = PolityWorld.RetailStockTarget - goodsStock + 5m;
          var px = PolityWorld.GoodsDelivered * (1m + (decimal)(_rng.NextDouble() * 0.04 - 0.02));
          _sim.Enqueue(new PostHubOrder(
            _ids.Station, loc, _ids.Goods, HubOrderSide.Buy, Quantity.From(need), Money.From(Math.Round(px, 2))));
          LastAction = $"bid Final ×{need:0} @ {site.Hub.Name}";
        }

        if (partsStock < PolityWorld.RetailStockTarget * 0.5m
            && site.Hub.Role is SystemRole.Capital or SystemRole.Inhabited)
        {
          var need = PolityWorld.RetailStockTarget * 0.5m - partsStock + 2m;
          var px = PolityWorld.PartsDelivered * (1m + (decimal)(_rng.NextDouble() * 0.04));
          _sim.Enqueue(new PostHubOrder(
            _ids.Station, loc, _ids.Parts, HubOrderSide.Buy, Quantity.From(need), Money.From(Math.Round(px, 2))));
        }
      }

      // Keep bunkers topped — prefer book fuel, else import. Offer surplus Energy for tramp bunkering.
      if (site.Hub.Role is SystemRole.Transit or SystemRole.Capital or SystemRole.Industrial or SystemRole.Mining)
      {
        var fuel = Qty(_ids.Station, loc, _ids.Fuel);
        var min = site.Hub.Role switch
        {
          SystemRole.Capital => 16m,
          SystemRole.Industrial => 12m,
          SystemRole.Transit => 14m,
          _ => 8m,
        };
        if (fuel < min)
        {
          var need = min - fuel + 4m;
          _sim.Enqueue(new PostHubOrder(
            _ids.Station, loc, _ids.Fuel, HubOrderSide.Buy, Quantity.From(need),
            Money.From(PolityWorld.FuelUnitCost * 1.1m)));
          var cash = world.Ledgers[_ids.Station].Cash.Amount;
          if (cash > PolityWorld.FuelUnitCost * 10m)
          {
            _sim.Enqueue(new PlaceProcurementOrder(
              _ids.Station, loc, _ids.Fuel, Quantity.From(Math.Max(8m, need)),
              Money.From(PolityWorld.FuelUnitCost)));
            LastAction = $"import Energy @ {site.Hub.Name}";
          }
        }
        else if (fuel > min + 6m)
        {
          var q = Math.Min(12m, fuel - min);
          _sim.Enqueue(new PostHubOrder(
            _ids.Station, loc, _ids.Fuel, HubOrderSide.Sell, Quantity.From(q),
            Money.From(PolityWorld.FuelUnitCost)));
        }
      }
    }
  }

  private decimal Qty(FirmId firm, InventoryLocationId loc, ProductId p) =>
    _sim.State.World.Inventory.GetQuantity(new InventoryKey(firm, loc, p)).Value;
}
