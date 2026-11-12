using TMS2000.Shared.DTO.Sync;

namespace TMS2000.Mobile.Services;



public class SnapshotStore
{
    public GameSnapshot? CurrentSnapshot { get; private set; }

    public event EventHandler? SnapshotUpdated;

    public SnapshotStore(TokenStore tokenStore)
    {
        tokenStore.SessionCleared += OnSessionCleared;
    }

    public void UpdateCurrentSnapshot(GameSnapshot snapshot)
    {
        CurrentSnapshot = snapshot;



        MainThread.BeginInvokeOnMainThread(() => SnapshotUpdated?.Invoke(this, EventArgs.Empty));
    }



    private void OnSessionCleared(object? sender, EventArgs eventArgs)
    {
        CurrentSnapshot = null;
    }
}