using SQLite;

namespace TMS2000.Mobile.Local;

public class LocalDatabase
{
    private const string DatabaseFileName = "tms2000.db3";

    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private SQLiteAsyncConnection? _connection;

    public async Task SaveSnapshotAsync(CachedSnapshot cachedSnapshot)
    {
        SQLiteAsyncConnection connection = await GetConnectionAsync();
        await connection.InsertOrReplaceAsync(cachedSnapshot);
    }

    public async Task<CachedSnapshot?> GetSnapshotAsync(string email)
    {
        SQLiteAsyncConnection connection = await GetConnectionAsync();
        return await connection.FindAsync<CachedSnapshot>(email);
    }

    public async Task AddPendingActionAsync(PendingAction pendingAction)
    {
        SQLiteAsyncConnection connection = await GetConnectionAsync();
        await connection.InsertAsync(pendingAction);
    }

    public async Task<List<PendingAction>> GetPendingActionsAsync(string email)
    {
        SQLiteAsyncConnection connection = await GetConnectionAsync();
        return await connection.Table<PendingAction>()
            .Where(pendingAction => pendingAction.Email == email)
            .OrderBy(pendingAction => pendingAction.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> CountPendingActionsAsync(string email)
    {
        SQLiteAsyncConnection connection = await GetConnectionAsync();
        return await connection.Table<PendingAction>()
            .Where(pendingAction => pendingAction.Email == email)
            .CountAsync();
    }

    public async Task RemovePendingActionsAsync(IEnumerable<string> transactionIds)
    {
        SQLiteAsyncConnection connection = await GetConnectionAsync();
        await connection.RunInTransactionAsync(transaction =>
        {
            foreach (string transactionId in transactionIds)
            {
                transaction.Delete<PendingAction>(transactionId);
            }
        });
    }

    public async Task SaveCredentialAsync(LocalCredential localCredential)
    {
        SQLiteAsyncConnection connection = await GetConnectionAsync();
        await connection.InsertOrReplaceAsync(localCredential);
    }

    public async Task<LocalCredential?> GetCredentialAsync(string email)
    {
        SQLiteAsyncConnection connection = await GetConnectionAsync();
        return await connection.FindAsync<LocalCredential>(email);
    }

    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_connection is not null)
        {
            return _connection;
        }

        await _initializationLock.WaitAsync();
        try
        {
            if (_connection is null)
            {
                string databasePath = Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);
                var connection = new SQLiteAsyncConnection(databasePath);
                await connection.CreateTableAsync<CachedSnapshot>();
                await connection.CreateTableAsync<PendingAction>();
                await connection.CreateTableAsync<LocalCredential>();
                _connection = connection;
            }

            return _connection;
        }
        finally
        {
            _initializationLock.Release();
        }
    }
}
