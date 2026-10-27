using System.Text.Json;
using TMS2000.Shared.DTO.Auth;

namespace TMS2000.Mobile.Services;

// Garde la session (tokens + rôle) dans SecureStorage, chiffré par le Keystore Android.
// Une copie reste en mémoire pour ne pas relire le stockage chiffré à chaque requête HTTP.

public class TokenStore
{
    private const string SessionStorageKey = "auth_session";

    private LoginResponse? _currentSession;
    private bool _hasLoadedFromStorage;

    public async Task<LoginResponse?> GetSessionAsync()
    {
        if (_hasLoadedFromStorage)
        {
            return _currentSession;
        }

        try
        {
            string? storedSessionJson = await SecureStorage.Default.GetAsync(SessionStorageKey);
            _currentSession = storedSessionJson is null
                ? null
                : JsonSerializer.Deserialize<LoginResponse>(storedSessionJson);
        }
        catch (Exception)
        {
            // Stockage illisible (ex. clé de chiffrement perdue après une réinstallation) :
            // on efface et on repart sans session, l'utilisateur se reconnectera.
            
            SecureStorage.Default.Remove(SessionStorageKey);
            _currentSession = null;
        }

        _hasLoadedFromStorage = true;
        return _currentSession;
    }

    public async Task SaveSessionAsync(LoginResponse session)
    {
        string sessionJson = JsonSerializer.Serialize(session);
        await SecureStorage.Default.SetAsync(SessionStorageKey, sessionJson);
        _currentSession = session;
        _hasLoadedFromStorage = true;
    }

    public void ClearSession()
    {
        SecureStorage.Default.Remove(SessionStorageKey);
        _currentSession = null;
        _hasLoadedFromStorage = true;
    }
}