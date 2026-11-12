using System.Text.Json;
using TMS2000.Shared.DTO.Auth;

namespace TMS2000.Mobile.Services;



public class TokenStore
{
    private const string SessionStorageKey = "auth_session";

    private LoginResponse? _currentSession;
    private bool _hasLoadedFromStorage;
    public event EventHandler? SessionCleared;

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
        SessionCleared?.Invoke(this, EventArgs.Empty);

        // alors ça veut dire : on a clean la session, invoke sessionCleared, l'event nommé "SessionCleared". qui signale à tout ceux en on besoin que la session est cleared.
        // les paramètre sont le sender, ici "this" donc le TokenStore. et l'argument est empty, on a pas de message à faire passer
    }
}