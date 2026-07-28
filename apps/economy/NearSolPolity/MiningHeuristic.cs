using Novolis.Economy;
using Novolis.Economy.Production;
using Novolis.Economy.Simulation;

namespace NearSolPolity;

/// <summary>Mining firm: throttle Raw, sell on hub book, buy Capital when low.</summary>
internal sealed class MiningHeuristic
{
  private readonly EconomySimulation _sim;
  private readonly PolityWorld.Ids _ids;
  private readonly DeterministicRandom _rng;

  public MiningHeuristic(EconomySimulation sim, PolityWorld.Ids ids)
  {
    _sim = sim;
    _ids = ids;
    _rng = new DeterministicRandom(sim.State.Seed ^ 0x4D494E45UL);
  }

  public string LastAction { get; private set; } = "mining idle";

  public void Tick()
  {
    BookQuotes.CancelOpen(_sim, _ids.Mining);
    var world = _sim.State.World;
    foreach (var site in _ids.Sites.Values.Where(s => s.Hub.Role == SystemRole.Mining && s.Facility is not null))
    {
      var loc = site.Hub.LocationId;
      var facility = site.Facility!.Value;
      var ore = Qty(_ids.Mining, loc, _ids.Ore);
      var parts = Qty(_ids.Mining, loc, _ids.Parts);
      var rate = parts < PolityWorld.PartsPerOre
        ? 0m
        : ProductionThrottle.Rate(2m, ore, PolityWorld.MineOreCap);
      _sim.Enqueue(new SetProductionPlan(_ids.Mining, facility, _ids.Ore, Quantity.From(rate)));

      if (ore > 10m)
      {
        var sellQty = Math.Min(20m, ore - 5m);
        var px = PolityWorld.OreBuy * (1m + (decimal)(_rng.NextDouble() * 0.06 - 0.03));
        _sim.Enqueue(new PostHubOrder(
          _ids.Mining, loc, _ids.Ore, HubOrderSide.Sell, Quantity.From(sellQty), Money.From(Math.Round(px, 2))));
        LastAction = $"sell Raw ×{sellQty:0} @ {site.Hub.Name}";
      }

      if (parts < PolityWorld.MinePartsFloor)
      {
        var need = PolityWorld.MinePartsFloor - parts + 4m;
        var px = PolityWorld.PartsDelivered * (1m + (decimal)(_rng.NextDouble() * 0.04));
        _sim.Enqueue(new PostHubOrder(
          _ids.Mining, loc, _ids.Parts, HubOrderSide.Buy, Quantity.From(need), Money.From(Math.Round(px, 2))));
        LastAction = $"bid Capital ×{need:0} @ {site.Hub.Name}";
      }

      if (rate <= 0m && parts < PolityWorld.PartsPerOre)
      {
        LastAction = $"mine starved (Capital) @ {site.Hub.Name}";
      }
      else if (rate <= 0m)
      {
        LastAction = $"mine idle (cap) @ {site.Hub.Name}";
      }
    }
  }

  private decimal Qty(FirmId firm, InventoryLocationId loc, ProductId p) =>
    _sim.State.World.Inventory.GetQuantity(new InventoryKey(firm, loc, p)).Value;
}
