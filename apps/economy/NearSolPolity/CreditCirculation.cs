using Novolis.Economy;
using Novolis.Economy.Accounting;
using Novolis.Economy.Population;
using Novolis.Economy.Simulation;

namespace NearSolPolity;

/// <summary>
/// Closed credit loop: firm wage cash-out becomes consumer spending power;
/// corridor tolls paid by shippers accrue to the co-op treasury.
/// Period budget resets are disabled (see <see cref="PolityWorld"/> PeriodHours).
/// </summary>
internal sealed class CreditCirculation
{
  private readonly EconomySimulation _sim;
  private readonly PolityWorld.Ids _ids;
  private int _eventCursor;
  private decimal _wagesDistributed;
  private decimal _importSpend;
  private decimal _tollsToPolity;

  public CreditCirculation(EconomySimulation sim, PolityWorld.Ids ids)
  {
    _sim = sim;
    _ids = ids;
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
    var world = _sim.State.World;
    for (var i = Math.Max(eventsBeforePulse, _eventCursor); i < events.Count; i++)
    {
      switch (events[i])
      {
        case WagesPaid e:
          DistributeWagesToHouseholds(e.Amount.Amount);
          _wagesDistributed += e.Amount.Amount;
          break;
        case ProcurementFilled e:
          // Exogenous fill = cash left the closed polity for an outside market.
          _importSpend += e.UnitPrice.Amount * e.Quantity.Value;
          break;
        case TransportTollPaid e:
          if (world.Ledgers.TryGetValue(_ids.Polity, out var treasury))
          {
            treasury.Post(
              AccountRole.Cash,
              AccountRole.Revenue,
              e.Amount,
              _sim.State.Clock.Date,
              "Corridor toll");
            _tollsToPolity += e.Amount.Amount;
          }

          break;
      }
    }

    _eventCursor = events.Count;
  }

  private void DistributeWagesToHouseholds(decimal amount)
  {
    if (amount <= 0m)
    {
      return;
    }

    var cohorts = _sim.State.World.Cohorts;
    if (cohorts.Count == 0)
    {
      return;
    }

    var popTotal = cohorts.Sum(c => Math.Max(1, c.Definition.Population.Value));
    if (popTotal <= 0)
    {
      return;
    }

    // Integer-safe split: give each cohort a population share; remainder to largest.
    var allocated = 0m;
    var ordered = cohorts.OrderByDescending(c => c.Definition.Population.Value)
      .ThenBy(c => c.Definition.Id.Value)
      .ToList();

    for (var i = 0; i < ordered.Count; i++)
    {
      var c = ordered[i];
      decimal share;
      if (i == ordered.Count - 1)
      {
        share = amount - allocated;
      }
      else
      {
        share = Math.Round(
          amount * c.Definition.Population.Value / popTotal,
          4,
          MidpointRounding.AwayFromZero);
        allocated += share;
      }

      if (share > 0m)
      {
        c.BudgetRemaining = Money.From(c.BudgetRemaining.Amount + share);
      }
    }
  }
}
