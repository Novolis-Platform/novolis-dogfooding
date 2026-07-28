using Novolis.Economy;
using Novolis.Economy.Markets;
using Novolis.Economy.Simulation;

namespace NearSolPolity;

internal static class BookQuotes
{
  /// <summary>Cancel open orders for this firm (+ optional location/product filter).</summary>
  public static void CancelOpen(
    EconomySimulation sim,
    FirmId firm,
    InventoryLocationId? location = null,
    ProductId? product = null)
  {
    var world = sim.State.World;
    foreach (var order in world.HubOrders
               .Where(o => o.FirmId.Equals(firm) && !o.IsFilled)
               .Where(o => location is null || o.LocationId.Equals(location.Value))
               .Where(o => product is null || o.ProductId.Equals(product.Value))
               .Select(o => o.Id)
               .ToList())
    {
      sim.Enqueue(new CancelHubOrder(order));
    }
  }
}
