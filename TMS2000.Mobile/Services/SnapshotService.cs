using TMS2000.Mobile.API;
using TMS2000.Shared.DTO.Sync;

namespace TMS2000.Mobile.Services;



public class SnapshotService
{
    private const string SyncRoute = "api/sync";
    private readonly ApiClient _apiClient;
    private readonly SnapshotStore _snapshotStore;

    public SnapshotService(ApiClient apiClient, SnapshotStore snapshotStore)
    {
        _apiClient = apiClient;
        _snapshotStore = snapshotStore;
    }


    public async Task<GameSnapshot> RefreshAsync(CancellationToken cancellationToken = default)
    {
        GameSnapshot snapshot = await _apiClient.GetAsync<GameSnapshot>(SyncRoute, cancellationToken);


        cancellationToken.ThrowIfCancellationRequested();

        _snapshotStore.UpdateCurrentSnapshot(snapshot);
        return snapshot;
    }
}