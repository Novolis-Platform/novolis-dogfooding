using Novolis.Economy;
using Novolis.Economy.Production;
using Novolis.Economy.Simulation;

namespace NearSolPolity;

/// <summary>Industry firm: buy Raw, produce Capital/Final/Energy, post sells.</summary>
internal sealed class IndustryHeuristic
{
  private readonly EconomySimulation _sim;
  private readonly PolityWorld.Ids _ids;
  private readonly DeterministicRandom _rng;

  public IndustryHeuristic(EconomySimulation sim, PolityWorld.Ids ids)
  {
    _sim = sim;
    _ids = ids;
    _rng = new DeterministicRandom(sim.State.Seed ^ 0x494E4455UL);
  }

  public string LastAction { get; private set; } = "industry idle";

  public void Tick()
  {
    BookQuotes.CancelOpen(_sim, _ids.Industry);
    var world = _sim.State.World;
    foreach (var site in _ids.Sites.Values.Where(s => s.Hub.Role == SystemRole.Industrial && s.Facility is not null))
    {
      var loc = site.Hub.LocationId;
      var facility = site.Facility!.Value;
      var ore = Qty(_ids.Industry, loc, _ids.Ore);
      var parts = Qty(_ids.Industry, loc, _ids.Parts);
      var goods = Qty(_ids.Industry, loc, _ids.Goods);
      var fuel = Qty(_ids.Industry, loc, _ids.Fuel);

      if (ore < PolityWorld.PlantOreFloor + 15m)
      {
        var need = Math.Max(8m, PolityWorld.PlantOreFloor - ore + 10m);
        var px = PolityWorld.OreDelivered * (1m + (decimal)(_rng.NextDouble() * 0.04 - 0.02));
        _sim.Enqueue(new PostHubOrder(
          _ids.Industry, loc, _ids.Ore, HubOrderSide.Buy, Quantity.From(need), Money.From(Math.Round(px, 2))));
      }

      var partsRate = ProductionThrottle.Rate(ore >= 1m ? 4m : 0m, parts, 40m);
      var goodsRate = ProductionThrottle.Rate(parts >= 1m ? 2.5m : 0m, goods, PolityWorld.RetailStockTarget * 1.5m);
      var fuelRate = ProductionThrottle.Rate(ore >= 10m ? 2m : 0m, fuel, 30m);
      _sim.Enqueue(new SetProductionPlan(_ids.Industry, facility, _ids.Parts, Quantity.From(partsRate)));
      _sim.Enqueue(new SetProductionPlan(_ids.Industry, facility, _ids.Goods, Quantity.From(goodsRate)));
      _sim.Enqueue(new SetProductionPlan(_ids.Industry, facility, _ids.Fuel, Quantity.From(fuelRate)));

      if (parts > 8m)
      {
        var q = Math.Min(15m, parts - 4m);
        var px = PolityWorld.PartsBuy * (1m + (decimal)(_rng.NextDouble() * 0.04 - 0.02));
        _sim.Enqueue(new PostHubOrder(
          _ids.Industry, loc, _ids.Parts, HubOrderSide.Sell, Quantity.From(q), Money.From(Math.Round(px, 2))));
      }

      if (goods > 6m)
      {
        var q = Math.Min(12m, goods - 2m);
        var px = PolityWorld.GoodsFactory * (1m + (decimal)(_rng.NextDouble() * 0.04 - 0.02));
        _sim.Enqueue(new PostHubOrder(
          _ids.Industry, loc, _ids.Goods, HubOrderSide.Sell, Quantity.From(q), Money.From(Math.Round(px, 2))));
      }

      if (fuel > 10m)
      {
        var q = Math.Min(12m, fuel - 4m);
        _sim.Enqueue(new PostHubOrder(
          _ids.Industry, loc, _ids.Fuel, HubOrderSide.Sell, Quantity.From(q), Money.From(PolityWorld.FuelUnitCost)));
      }

      LastAction = $"plant @ {site.Hub.Name} p{partsRate:0}/g{goodsRate:0}/f{fuelRate:0}";
    }
  }

  private decimal Qty(FirmId firm, InventoryLocationId loc, ProductId p) =>
    _sim.State.World.Inventory.GetQuantity(new InventoryKey(firm, loc, p)).Value;
}
