using TMS2000.Mobile.API;
using TMS2000.Shared.DTO.Auth;

namespace TMS2000.Mobile.Services;

// Connexion, déconnexion et reprise de session : utilisé par les ViewModels.
// Ne garde aucun état lui-même (l'état est dans TokenStore).

// side-node => c'est la couche "service front login".
public class SessionService
{
    private readonly AuthApiClient _authApiClient;
    private readonly TokenStore _tokenStore;

    public SessionService(AuthApiClient authApiClient, TokenStore tokenStore)
    {
        _authApiClient = authApiClient;
        _tokenStore = tokenStore;
    }


    public async Task<string> LoginAsync(string email, string password)
    {
        LoginResponse session = await _authApiClient.LoginAsync(email, password);
        await _tokenStore.SaveSessionAsync(session);
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

        if (currentSession is null)
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
}