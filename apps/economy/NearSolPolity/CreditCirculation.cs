using Novolis.Economy;
using Novolis.Economy.Finance;
using Novolis.Economy.Production;
using Novolis.Economy.Simulation;

namespace NearSolPolity;

/// <summary>Dashboard metrics for liquid stock, imports, finance, and activity (incremental event scan).</summary>
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
  private decimal _interestAccrued;
  private decimal _interestPaid;
  private int _loansOriginated;
  private int _loansDefaulted;
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
  public decimal InterestAccrued => _interestAccrued;
  public decimal InterestPaid => _interestPaid;
  public int LoansOriginated => _loansOriginated;
  public int LoansDefaulted => _loansDefaulted;
  public IReadOnlyDictionary<string, int> PlanFailReasons => _planFailReasons;

  public decimal LiquidStock => MoneyStock.Liquid(_sim.State.World);

  public int ActiveLoans =>
    _sim.State.World.Loans.Count(l => l.Status == LoanStatus.Active);

  public decimal PrincipalOutstanding =>
    _sim.State.World.Loans.Where(l => l.Status is LoanStatus.Active or LoanStatus.Defaulted)
      .Sum(l => l.PrincipalRemaining.Amount);

  public decimal InventoryBookValue
  {
    get
    {
      var inv = _sim.State.World.Inventory;
      var sum = 0m;
      foreach (var key in inv.Keys)
      {
        foreach (var lot in inv.GetLots(key))
        {
          sum += lot.Quantity.Value * lot.UnitCost.Amount;
        }
      }

      return sum;
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
        case InterestAccrued e:
          _interestAccrued += e.Amount.Amount;
          break;
        case LoanRepaid e:
          _interestPaid += e.Amount.Amount;
          break;
        case LoanOriginated:
          _loansOriginated++;
          break;
        case LoanDefaulted:
          _loansDefaulted++;
          break;
      }
    }

    _eventCursor = events.Count;
  }
}
