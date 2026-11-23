using TMS2000.Mobile.Local;
using TMS2000.Shared;
using TMS2000.Shared.DTO.Players;

namespace TMS2000.Mobile.Services;

public class PendingActionQueue
{
    private readonly LocalDatabase _localDatabase;
    private readonly object _pendingActionsLock = new();
    private List<PendingAction> _pendingActions = new();

    public PendingActionQueue(LocalDatabase localDatabase, TokenStore tokenStore)
    {
        _localDatabase = localDatabase;
        tokenStore.SessionCleared += OnSessionCleared;
    }

    public event EventHandler? PendingActionsChanged;

    public int Count
    {
        get
        {
            lock (_pendingActionsLock)
            {
                return _pendingActions.Count;
            }
        }
    }

    public async Task LoadAsync(string email)
    {
        List<PendingAction> storedPendingActions = await _localDatabase.GetPendingActionsAsync(email);
        lock (_pendingActionsLock)
        {
            _pendingActions = storedPendingActions;
        }

        RaisePendingActionsChanged();
    }

    public async Task EnqueueAsync(PendingAction pendingAction)
    {
        await _localDatabase.AddPendingActionAsync(pendingAction);
        lock (_pendingActionsLock)
        {
            _pendingActions.Add(pendingAction);
        }

        RaisePendingActionsChanged();
    }

    public async Task RemoveAsync(IReadOnlyCollection<string> transactionIds)
    {
        await _localDatabase.RemovePendingActionsAsync(transactionIds);
        lock (_pendingActionsLock)
        {
            _pendingActions.RemoveAll(pendingAction => transactionIds.Contains(pendingAction.TransactionId));
        }

        RaisePendingActionsChanged();
    }

    public List<PendingAction> GetPendingActions()
    {
        lock (_pendingActionsLock)
        {
            return _pendingActions.OrderBy(pendingAction => pendingAction.CreatedAt).ToList();
        }
    }

    public List<TransactionDto> MergeWithPendingTransactions(IEnumerable<TransactionDto> serverTransactions) =>
        serverTransactions
            .Concat(GetPendingActions().Select(pendingAction => pendingAction.ToTransactionDto()))
            .DistinctBy(transaction => transaction.TransactionId)
            .ToList();

    public decimal EstimatedPendingBuyCost()
    {
        lock (_pendingActionsLock)
        {
            return _pendingActions
                .Where(pendingAction => pendingAction.Type == TransactionType.Buy)
                .Sum(pendingAction => pendingAction.Quantity * pendingAction.EstimatedPrice);
        }
    }

    public int PendingSellQuantity(string resourceId)
    {
        lock (_pendingActionsLock)
        {
            return _pendingActions
                .Where(pendingAction => pendingAction.Type == TransactionType.Sell && pendingAction.ResourceId == resourceId)
                .Sum(pendingAction => pendingAction.Quantity);
        }
    }

    private void OnSessionCleared(object? sender, EventArgs eventArgs)
    {
        lock (_pendingActionsLock)
        {
            _pendingActions = new List<PendingAction>();
        }

        RaisePendingActionsChanged();
    }

    private void RaisePendingActionsChanged()
    {
        MainThread.BeginInvokeOnMainThread(() => PendingActionsChanged?.Invoke(this, EventArgs.Empty));
    }
}
