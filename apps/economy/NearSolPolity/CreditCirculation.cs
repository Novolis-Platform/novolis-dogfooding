using Novolis.Economy;
using Novolis.Economy.Simulation;

namespace NearSolPolity;

/// <summary>Dashboard metrics for liquid stock, imports, and activity (incremental event scan).</summary>
internal sealed class CreditCirculation
{
  private readonly EconomySimulation _sim;
  private int _eventCursor;
  private decimal _wagesDistributed;
  private decimal _importSpend;
  private decimal _tollsToTreasury;
  private decimal _produced;
  private decimal _retailSold;
  private int _bookFills;
  private decimal _bookFillQty;
  private int _departed;
  private readonly Dictionary<string, int> _planFailReasons = new(StringComparer.Ordinal);

  public CreditCirculation(EconomySimulation sim)
  {
    _sim = sim;
    _eventCursor = sim.State.Events.Count;
  }

  public decimal WagesDistributed => _wagesDistributed;
  public decimal ImportSpend => _importSpend;
  public decimal TollsToTreasury => _tollsToTreasury;
  public decimal Produced => _produced;
  public decimal RetailSold => _retailSold;
  public int BookFills => _bookFills;
  public decimal BookFillQty => _bookFillQty;
  public int Departed => _departed;
  public IReadOnlyDictionary<string, int> PlanFailReasons => _planFailReasons;

  public decimal LiquidStock => MoneyStock.Liquid(_sim.State.World);

  public void ObserveAfterPulse(int eventsBeforePulse)
  {
    var events = _sim.State.Events;
    for (var i = Math.Max(eventsBeforePulse, _eventCursor); i < events.Count; i++)
    {
      switch (events[i])
      {
        case HouseholdCreditsIssued e:
          _wagesDistributed += e.Amount.Amount;
          break;
        case ProcurementFilled e:
          _importSpend += e.UnitPrice.Amount * e.Quantity.Value;
          break;
        case TransportTollPaid e:
          _tollsToTreasury += e.Amount.Amount;
          break;
        case BatchProduced e:
          _produced += e.Quantity.Value;
          break;
        case GoodsSold e:
          _retailSold += e.Quantity.Value;
          break;
        case HubOrderFilled e:
          _bookFills++;
          _bookFillQty += e.Quantity.Value;
          break;
        case ShipmentDeparted:
          _departed++;
          break;
        case ShipmentPlanFailed e:
          _planFailReasons[e.Reason] = _planFailReasons.GetValueOrDefault(e.Reason) + 1;
          break;
      }
    }

    _eventCursor = events.Count;
  }
}
