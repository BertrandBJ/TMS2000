using TMS2000.Mobile.API;
using TMS2000.Mobile.Local;
using TMS2000.Shared;
using TMS2000.Shared.DTO.Sync;

namespace TMS2000.Mobile.Services;

public class PendingActionSynchronizer
{
    private const string SyncRoute = "api/sync";
    private const int MaxActionsPerSync = 100;

    private readonly IServiceProvider _serviceProvider;
    private readonly PendingActionQueue _pendingActionQueue;
    private readonly SnapshotStore _snapshotStore;
    private readonly ConnectionStatus _connectionStatus;
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    public PendingActionSynchronizer(
        IServiceProvider serviceProvider,
        PendingActionQueue pendingActionQueue,
        SnapshotStore snapshotStore,
        ConnectionStatus connectionStatus)
    {
        _serviceProvider = serviceProvider;
        _pendingActionQueue = pendingActionQueue;
        _snapshotStore = snapshotStore;
        _connectionStatus = connectionStatus;
    }

    public async Task SyncAsync(CancellationToken cancellationToken = default)
    {
        await _syncLock.WaitAsync(cancellationToken);
        try
        {
            List<PendingAction> pendingActions = _pendingActionQueue.GetPendingActions();
            if (pendingActions.Count == 0)
            {
                return;
            }

            var apiClient = _serviceProvider.GetRequiredService<ApiClient>();
            int acceptedCount = 0;
            int rejectedCount = 0;

            foreach (PendingAction[] pendingActionBatch in pendingActions.Chunk(MaxActionsPerSync))
            {
                var syncPushRequest = new SyncPushRequest(
                    pendingActionBatch.Select(pendingAction => pendingAction.ToOfflineAction()).ToList());

                SyncPushResponse syncPushResponse =
                    await apiClient.PostAsync<SyncPushRequest, SyncPushResponse>(SyncRoute, syncPushRequest);

                List<string> processedTransactionIds = syncPushResponse.Results
                    .Select(actionResult => actionResult.TransactionId)
                    .ToList();
                await _pendingActionQueue.RemoveAsync(processedTransactionIds);
                _snapshotStore.UpdateCurrentSnapshot(syncPushResponse.Snapshot);

                acceptedCount += syncPushResponse.Results.Count(actionResult => actionResult.Status == TransactionStatus.Accepted);
                rejectedCount += syncPushResponse.Results.Count(actionResult => actionResult.Status == TransactionStatus.Rejected);
            }

            _connectionStatus.MarkReachable();
            _connectionStatus.ReportSyncResult(acceptedCount, rejectedCount);
        }
        finally
        {
            _syncLock.Release();
        }
    }
}
