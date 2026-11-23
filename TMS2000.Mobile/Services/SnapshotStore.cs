using System.Text.Json;
using TMS2000.Mobile.Local;
using TMS2000.Shared.DTO.Sync;

namespace TMS2000.Mobile.Services;



public class SnapshotStore
{
    private static readonly JsonSerializerOptions CacheJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly LocalDatabase _localDatabase;
    private readonly CurrentAccount _currentAccount;

    public GameSnapshot? CurrentSnapshot { get; private set; }

    public DateTime? ReceivedAt { get; private set; }

    public TimeSpan ServerClockOffset { get; private set; }

    public DateTime EstimatedServerTime => DateTime.UtcNow + ServerClockOffset;

    public event EventHandler? SnapshotUpdated;

    public SnapshotStore(TokenStore tokenStore, LocalDatabase localDatabase, CurrentAccount currentAccount)
    {
        _localDatabase = localDatabase;
        _currentAccount = currentAccount;
        tokenStore.SessionCleared += OnSessionCleared;
    }

    public void UpdateCurrentSnapshot(GameSnapshot snapshot)
    {
        DateTime receivedAt = DateTime.UtcNow;
        TimeSpan serverClockOffset = AsUtc(snapshot.ServerTime) - receivedAt;

        CurrentSnapshot = snapshot;
        ReceivedAt = receivedAt;
        ServerClockOffset = serverClockOffset;
        _currentAccount.SetEmail(snapshot.Player.Email);
        _ = SaveToCacheAsync(snapshot, receivedAt, serverClockOffset);



        MainThread.BeginInvokeOnMainThread(() => SnapshotUpdated?.Invoke(this, EventArgs.Empty));
    }

    public async Task LoadFromCacheAsync(string email)
    {
        if (CurrentSnapshot is not null)
        {
            return;
        }

        CachedSnapshot? cachedSnapshot = await _localDatabase.GetSnapshotAsync(CurrentAccount.Normalize(email));
        if (cachedSnapshot is null || CurrentSnapshot is not null)
        {
            return;
        }

        GameSnapshot? snapshot = JsonSerializer.Deserialize<GameSnapshot>(cachedSnapshot.SnapshotJson, CacheJsonOptions);
        if (snapshot is null)
        {
            return;
        }

        CurrentSnapshot = snapshot;
        ReceivedAt = DateTime.SpecifyKind(cachedSnapshot.SavedAt, DateTimeKind.Utc);
        ServerClockOffset = TimeSpan.FromSeconds(cachedSnapshot.ServerClockOffsetSeconds);
        MainThread.BeginInvokeOnMainThread(() => SnapshotUpdated?.Invoke(this, EventArgs.Empty));
    }

    private async Task SaveToCacheAsync(GameSnapshot snapshot, DateTime receivedAt, TimeSpan serverClockOffset)
    {
        try
        {
            var cachedSnapshot = new CachedSnapshot
            {
                Email = CurrentAccount.Normalize(snapshot.Player.Email),
                SnapshotJson = JsonSerializer.Serialize(snapshot, CacheJsonOptions),
                SavedAt = receivedAt,
                ServerClockOffsetSeconds = serverClockOffset.TotalSeconds
            };
            await _localDatabase.SaveSnapshotAsync(cachedSnapshot);
        }
        catch (Exception)
        {
        }
    }

    private static DateTime AsUtc(DateTime serverTime) => serverTime.Kind == DateTimeKind.Local
        ? serverTime.ToUniversalTime()
        : DateTime.SpecifyKind(serverTime, DateTimeKind.Utc);

    private void OnSessionCleared(object? sender, EventArgs eventArgs)
    {
        CurrentSnapshot = null;
        ReceivedAt = null;
        ServerClockOffset = TimeSpan.Zero;
    }
}
