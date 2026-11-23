using TMS2000.Mobile.API;
using TMS2000.Mobile.Local;
using TMS2000.Shared;
using TMS2000.Shared.DTO.Auth;

namespace TMS2000.Mobile.Services;

// Connexion, déconnexion et reprise de session : utilisé par les ViewModels.
// Ne garde aucun état lui-même (l'état est dans TokenStore).

// side-node => c'est la couche "service front login".
public class SessionService
{
    private const int PasswordHashWorkFactor = 10;
    private const string OfflineSessionToken = "offline";
    private static readonly TimeSpan OfflineSessionDuration = TimeSpan.FromDays(3);

    private readonly AuthApiClient _authApiClient;
    private readonly TokenStore _tokenStore;
    private readonly LocalDatabase _localDatabase;
    private readonly CurrentAccount _currentAccount;

    public SessionService(
        AuthApiClient authApiClient,
        TokenStore tokenStore,
        LocalDatabase localDatabase,
        CurrentAccount currentAccount)
    {
        _authApiClient = authApiClient;
        _tokenStore = tokenStore;
        _localDatabase = localDatabase;
        _currentAccount = currentAccount;
    }


    public async Task<string> LoginAsync(string email, string password)
    {
        LoginResponse session;
        try
        {
            session = await _authApiClient.LoginAsync(email, password);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            if (await TryOfflineLoginAsync(email, password))
            {
                return Roles.Player;
            }

            throw;
        }

        await _tokenStore.SaveSessionAsync(session);
        _currentAccount.SetEmail(email);
        await SaveLocalCredentialAsync(email, password, session.Role);
        return session.Role;
    }



    public async Task<string?> GetSavedSessionRoleAsync()
    {
        LoginResponse? savedSession = await _tokenStore.GetSessionAsync();
        if (savedSession is null)
        {
            return null;
        }

        if (savedSession.RefreshTokenExpirationDate <= DateTime.UtcNow)
        {
            // Le refresh token est périmé : impossible de renouveler, autant repasser par le login.
            _tokenStore.ClearSession();
            return null;
        }

        return savedSession.Role;
    }

    public async Task LogoutAsync()
    {
        LoginResponse? currentSession = await _tokenStore.GetSessionAsync();


        _tokenStore.ClearSession();

        if (currentSession is null || currentSession.RefreshToken == OfflineSessionToken)
        {
            return;
        }

        try
        {
            await _authApiClient.LogoutAsync(currentSession.RefreshToken);
        }
        catch (Exception)
        {
            // Serveur injoignable ou token déjà révoqué : rien de plus à faire,
            // le refresh token expirera de lui-même côté serveur.
        }
    }

    private async Task<bool> TryOfflineLoginAsync(string email, string password)
    {
        string normalizedEmail = CurrentAccount.Normalize(email);

        LocalCredential? localCredential;
        try
        {
            localCredential = await _localDatabase.GetCredentialAsync(normalizedEmail);
        }
        catch (Exception)
        {
            return false;
        }

        if (localCredential is null || localCredential.Role != Roles.Player)
        {
            return false;
        }

        bool isPasswordValid = await Task.Run(() => BCrypt.Net.BCrypt.Verify(password, localCredential.PasswordHash));
        if (!isPasswordValid)
        {
            throw new OfflineLoginException(
                "Serveur injoignable, et ce mot de passe ne correspond pas à la dernière connexion faite sur ce téléphone.");
        }

        var offlineSession = new LoginResponse(
            OfflineSessionToken,
            DateTime.UtcNow,
            OfflineSessionToken,
            DateTime.UtcNow + OfflineSessionDuration,
            Roles.Player);

        await _tokenStore.SaveSessionAsync(offlineSession);
        _currentAccount.SetEmail(normalizedEmail);
        return true;
    }

    private async Task SaveLocalCredentialAsync(string email, string password, string role)
    {
        if (role != Roles.Player)
        {
            return;
        }

        try
        {
            string passwordHash = await Task.Run(() => BCrypt.Net.BCrypt.HashPassword(password, PasswordHashWorkFactor));
            var localCredential = new LocalCredential
            {
                Email = CurrentAccount.Normalize(email),
                PasswordHash = passwordHash,
                Role = role,
                LastOnlineLoginAt = DateTime.UtcNow
            };
            await _localDatabase.SaveCredentialAsync(localCredential);
        }
        catch (Exception)
        {
        }
    }
}
