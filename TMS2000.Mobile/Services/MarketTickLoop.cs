using TMS2000.Mobile.API;
using TMS2000.Mobile.Local;
using TMS2000.Shared.DTO.Sync;

namespace TMS2000.Mobile.Services;

// Rafraîchit le snapshot juste après chaque tick de prix du serveur :
// les écrans du joueur suivent le marché sans que l'utilisateur ait à tirer pour rafraîchir.
// Singleton : une seule boucle pour toute l'app. Démarrée par AppNavigator quand le PlayerShell s'affiche,
// arrêtée à la déconnexion (SessionCleared). Jamais dans un ViewModel : elle survivrait au logout.

public class MarketTickLoop
{
    private static readonly TimeSpan DelayAfterTick = TimeSpan.FromSeconds(2);

    // Hors ligne, serveur injoignable, retry
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);

    private readonly IServiceProvider _serviceProvider;
    private readonly CurrentAccount _currentAccount;
    private readonly SnapshotStore _snapshotStore;
    private readonly PendingActionQueue _pendingActionQueue;
    private readonly PendingActionSynchronizer _pendingActionSynchronizer;
    private readonly ConnectionStatus _connectionStatus;
    private CancellationTokenSource? _loopCancellation;
    private bool _isPaused;

    public MarketTickLoop(
        IServiceProvider serviceProvider,
        TokenStore tokenStore,
        CurrentAccount currentAccount,
        SnapshotStore snapshotStore,
        PendingActionQueue pendingActionQueue,
        PendingActionSynchronizer pendingActionSynchronizer,
        ConnectionStatus connectionStatus)
    {
        _serviceProvider = serviceProvider;
        _currentAccount = currentAccount;
        _snapshotStore = snapshotStore;
        _pendingActionQueue = pendingActionQueue;
        _pendingActionSynchronizer = pendingActionSynchronizer;
        _connectionStatus = connectionStatus;
        tokenStore.SessionCleared += OnSessionCleared;
    }

    public void Start()
    {
        _isPaused = false;
        if (_loopCancellation is not null)
        {
            return;
        }

        _loopCancellation = new CancellationTokenSource();
        CancellationToken loopCancellationToken = _loopCancellation.Token;
        _ = Task.Run(() => RunAsync(loopCancellationToken));
    }

    public void Stop()
    {

        _loopCancellation?.Cancel();
        _loopCancellation = null;
    }

    public void Pause()
    {
        if (_loopCancellation is null)
        {
            return;
        }

        Stop();
        _isPaused = true;
    }

    public void Resume()
    {
        if (!_isPaused)
        {
            return;
        }

        Start();
    }

    private void OnSessionCleared(object? sender, EventArgs eventArgs)
    {
        _isPaused = false;
        Stop();
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            await RestoreLocalStateAsync();
            while (true)
            {
                TimeSpan waitTime = await RefreshAndGetWaitTimeAsync(cancellationToken);
                await Task.Delay(waitTime, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {

        }
    }

    private async Task RestoreLocalStateAsync()
    {
        string? email = _currentAccount.Email;
        if (email is null)
        {
            return;
        }

        try
        {
            await _pendingActionQueue.LoadAsync(email);
            await _snapshotStore.LoadFromCacheAsync(email);
        }
        catch (Exception)
        {
        }
    }


    private async Task<TimeSpan> RefreshAndGetWaitTimeAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_pendingActionQueue.Count > 0)
            {
                await _pendingActionSynchronizer.SyncAsync(cancellationToken);
            }

            var snapshotService = _serviceProvider.GetRequiredService<SnapshotService>();
            GameSnapshot snapshot = await snapshotService.RefreshAsync(cancellationToken);
            _connectionStatus.MarkReachable();

            TimeSpan timeUntilNextTick = snapshot.NextTickAt - snapshot.ServerTime;

            if (timeUntilNextTick < TimeSpan.Zero)
            {
                timeUntilNextTick = TimeSpan.Zero;
            }

            return timeUntilNextTick + DelayAfterTick;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (ApiException)
        {
            _connectionStatus.MarkReachable();
            return RetryDelay;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            _connectionStatus.MarkUnreachable();
            return RetryDelay;
        }
        catch (Exception)
        {
            return RetryDelay;
        }
    }
}
