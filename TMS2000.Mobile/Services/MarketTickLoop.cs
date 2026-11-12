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
    private CancellationTokenSource? _loopCancellation;

    public MarketTickLoop(IServiceProvider serviceProvider, TokenStore tokenStore)
    {
        _serviceProvider = serviceProvider;
        tokenStore.SessionCleared += OnSessionCleared;
    }

    public void Start()
    {
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

    private void OnSessionCleared(object? sender, EventArgs eventArgs)
    {
        Stop();
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
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


    private async Task<TimeSpan> RefreshAndGetWaitTimeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var snapshotService = _serviceProvider.GetRequiredService<SnapshotService>();
            GameSnapshot snapshot = await snapshotService.RefreshAsync(cancellationToken);

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
        catch (Exception)
        {
            return RetryDelay;
        }
    }
}
