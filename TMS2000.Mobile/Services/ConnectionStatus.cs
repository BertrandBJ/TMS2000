namespace TMS2000.Mobile.Services;

public class ConnectionStatus
{
    private volatile bool _isServerUnreachable;

    public ConnectionStatus(TokenStore tokenStore)
    {
        tokenStore.SessionCleared += OnSessionCleared;
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
    }

    public event EventHandler? StatusChanged;

    public bool IsOffline => _isServerUnreachable || Connectivity.Current.NetworkAccess == NetworkAccess.None;

    public string? SyncMessage { get; private set; }

    public void MarkReachable() => SetServerUnreachable(false);

    public void MarkUnreachable() => SetServerUnreachable(true);

    public void ReportSyncResult(int acceptedCount, int rejectedCount)
    {
        string acceptedDescription = acceptedCount <= 1 ? $"{acceptedCount} exécuté" : $"{acceptedCount} exécutés";
        string rejectedDescription = rejectedCount <= 1 ? $"{rejectedCount} refusé" : $"{rejectedCount} refusés";
        SyncMessage = $"Ordres hors ligne envoyés : {acceptedDescription}, {rejectedDescription} (détail dans l'Historique).";
        RaiseStatusChanged();
    }

    private void SetServerUnreachable(bool isServerUnreachable)
    {
        if (_isServerUnreachable == isServerUnreachable)
        {
            return;
        }

        _isServerUnreachable = isServerUnreachable;
        RaiseStatusChanged();
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs eventArgs)
    {
        RaiseStatusChanged();
    }

    private void OnSessionCleared(object? sender, EventArgs eventArgs)
    {
        _isServerUnreachable = false;
        SyncMessage = null;
        RaiseStatusChanged();
    }

    private void RaiseStatusChanged()
    {
        MainThread.BeginInvokeOnMainThread(() => StatusChanged?.Invoke(this, EventArgs.Empty));
    }
}
