using Novolis.Economy;
using Novolis.Economy.Simulation;

namespace NearSolPolity;

/// <summary>
/// Dashboard metrics for liquid stock and import leakage.
/// Wage→household credits and toll treasury live in the Economy kernel
/// (<see cref="EconomyPolicy.HouseholdCreditFromWages"/>, <see cref="EconomyPolicy.TollBeneficiaryFirmId"/>).
/// </summary>
internal sealed class CreditCirculation
{
  private readonly EconomySimulation _sim;
  private int _eventCursor;
  private decimal _wagesDistributed;
  private decimal _importSpend;
  private decimal _tollsToPolity;

  public CreditCirculation(EconomySimulation sim)
  {
    _sim = sim;
    _eventCursor = sim.State.Events.Count;
  }

  public decimal WagesDistributed => _wagesDistributed;
  public decimal ImportSpend => _importSpend;
  public decimal TollsToPolity => _tollsToPolity;

  /// <summary>Firm cash + household budgets (closed liquid stock, excluding imports burned).</summary>
  public decimal LiquidStock
  {
    get
    {
      var world = _sim.State.World;
      var firms = world.Ledgers.Values.Sum(l => l.Cash.Amount);
      var households = world.Cohorts.Sum(c => c.BudgetRemaining.Amount);
      return firms + households;
    }
  }

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
          _tollsToPolity += e.Amount.Amount;
          break;
      }
    }

    _eventCursor = events.Count;
  }
}
