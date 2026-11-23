using TMS2000.Mobile.API;
using TMS2000.Mobile.Helpers;
using TMS2000.Mobile.Local;
using TMS2000.Shared;
using TMS2000.Shared.DTO.Sync;
using TMS2000.Shared.DTO.Trades;

namespace TMS2000.Mobile.Services;

public class TradeService
{
    public const int MaxQuantityPerTrade = 1_000_000;

    private const string TradesRoute = "api/trades";
    private readonly ApiClient _apiClient;
    private readonly SnapshotService _snapshotService;
    private readonly SnapshotStore _snapshotStore;
    private readonly PendingActionQueue _pendingActionQueue;
    private readonly PendingActionSynchronizer _pendingActionSynchronizer;
    private readonly ConnectionStatus _connectionStatus;
    private readonly CurrentAccount _currentAccount;

    public TradeService(
        ApiClient apiClient,
        SnapshotService snapshotService,
        SnapshotStore snapshotStore,
        PendingActionQueue pendingActionQueue,
        PendingActionSynchronizer pendingActionSynchronizer,
        ConnectionStatus connectionStatus,
        CurrentAccount currentAccount)
    {
        _apiClient = apiClient;
        _snapshotService = snapshotService;
        _snapshotStore = snapshotStore;
        _pendingActionQueue = pendingActionQueue;
        _pendingActionSynchronizer = pendingActionSynchronizer;
        _connectionStatus = connectionStatus;
        _currentAccount = currentAccount;
    }

    public async Task<TradeResult> ExecuteTradeAsync(string resourceId, TransactionType transactionType, int quantity)
    {
        string transactionId = Guid.NewGuid().ToString();

        if (_connectionStatus.IsOffline)
        {
            PendingAction queuedAction = await QueueTradeAsync(transactionId, resourceId, transactionType, quantity);
            return new TradeResult(null, queuedAction);
        }

        try
        {
            if (_pendingActionQueue.Count > 0)
            {
                await _pendingActionSynchronizer.SyncAsync();
            }

            var tradeRequest = new TradeRequest(transactionId, resourceId, transactionType, quantity);
            TradeResponse tradeResponse = await _apiClient.PostAsync<TradeRequest, TradeResponse>(TradesRoute, tradeRequest);
            _connectionStatus.MarkReachable();

            await RefreshSnapshotAfterTradeAsync();
            return new TradeResult(tradeResponse, null);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            _connectionStatus.MarkUnreachable();
            PendingAction queuedAction = await QueueTradeAsync(transactionId, resourceId, transactionType, quantity);
            return new TradeResult(null, queuedAction);
        }
    }

    public async Task<int> ComputeMaxBuyQuantityAsync(string resourceId)
    {
        GameSnapshot? latestSnapshot = await GetLatestSnapshotAsync();

        SnapshotResourceDto? resource = latestSnapshot?.Resources.FirstOrDefault(candidate => candidate.Id == resourceId);
        if (latestSnapshot is null || resource is null)
        {
            return 0;
        }

        decimal taxRate = RuleEvaluator.TaxRate(resource.Rules, latestSnapshot.ServerTime, resource.CurrentPrice);
        decimal unitCostWithTax = resource.CurrentPrice * (1 + taxRate);
        if (unitCostWithTax <= 0)
        {
            return 0;
        }

        decimal availableBalance = latestSnapshot.Player.Balance - _pendingActionQueue.EstimatedPendingBuyCost();
        if (availableBalance <= 0)
        {
            return 0;
        }

        int affordableQuantity = (int)Math.Min(
            Math.Floor(availableBalance / unitCostWithTax),
            MaxQuantityPerTrade);

        int? quantityLimit = RuleEvaluator.MaxQuantity(resource.Rules, latestSnapshot.ServerTime, resource.CurrentPrice);
        return quantityLimit is int limit ? Math.Min(affordableQuantity, limit) : affordableQuantity;
    }

    private async Task<GameSnapshot?> GetLatestSnapshotAsync()
    {
        if (_connectionStatus.IsOffline)
        {
            return _snapshotStore.CurrentSnapshot;
        }

        try
        {
            GameSnapshot freshSnapshot = await _snapshotService.RefreshAsync();
            _connectionStatus.MarkReachable();
            return freshSnapshot;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            _connectionStatus.MarkUnreachable();
            return _snapshotStore.CurrentSnapshot;
        }
    }

    private async Task<PendingAction> QueueTradeAsync(
        string transactionId, string resourceId, TransactionType transactionType, int quantity)
    {
        string email = _currentAccount.Email
                       ?? throw new InvalidOperationException("Compte inconnu : impossible d'enregistrer l'ordre hors ligne.");

        SnapshotResourceDto? resource = _snapshotStore.CurrentSnapshot?.Resources
            .FirstOrDefault(candidate => candidate.Id == resourceId);

        var pendingAction = new PendingAction
        {
            TransactionId = transactionId,
            Email = email,
            ResourceId = resourceId,
            ResourceName = resource?.Name ?? resourceId,
            Type = transactionType,
            Quantity = quantity,
            CreatedAt = _snapshotStore.EstimatedServerTime,
            EstimatedPrice = resource?.CurrentPrice ?? 0
        };

        await _pendingActionQueue.EnqueueAsync(pendingAction);
        return pendingAction;
    }

    private async Task RefreshSnapshotAfterTradeAsync()
    {
        try
        {
            await _snapshotService.RefreshAsync();
        }
        catch (Exception)
        {
        }
    }
}
