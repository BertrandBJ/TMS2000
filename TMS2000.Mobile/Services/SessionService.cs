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

    // Renvoie le rôle ("Player" ou "Admin") pour que l'app ouvre le bon Shell.
    public async Task<string> LoginAsync(string email, string password)
    {
        LoginResponse session = await _authApiClient.LoginAsync(email, password);
        await _tokenStore.SaveSessionAsync(session);
        return session.Role;
    }

    // Au démarrage de l'app : rôle de la session sauvegardée, ou null s'il faut afficher le login.
    
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

        // D'abord en local : l'utilisateur est déconnecté même si le serveur ne répond pas.
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