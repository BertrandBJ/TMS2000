using System.Net;
using System.Net.Http.Headers;
using TMS2000.Shared.DTO.Auth;
using TMS2000.Mobile.Services;

namespace TMS2000.Mobile.API;

// Intercepteur placé devant le client authentifié (ApiClient) :
// 1. ajoute "Authorization: Bearer <accessToken>" à chaque requête ;
// 2. si le serveur répond 401 (token expiré), fait un refresh puis rejoue la requête une fois.

public class AuthHandler : DelegatingHandler
{
    // static : partagé par toutes les instances de AuthHandler → un seul refresh à la fois dans toute l'app.
    
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);
    private readonly TokenStore _tokenStore;
    private readonly AuthApiClient _authApiClient;

    public AuthHandler(TokenStore tokenStore, AuthApiClient authApiClient)
    {
        _tokenStore = tokenStore;
        _authApiClient = authApiClient;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LoginResponse? session = await _tokenStore.GetSessionAsync();
        if (session is null)
        {
            // Pas connecté : on envoie sans token, le serveur répondra 401.
            
            return await base.SendAsync(request, cancellationToken);
        }

        SetBearerToken(request, session.AccessToken);
        
        
        HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        string? refreshedAccessToken = await TryRefreshAsync(session.AccessToken, cancellationToken);
        if (refreshedAccessToken is null)
        {
            // Refresh impossible : on laisse remonter le 401 d'origine.
            
            return response;
        }

        response.Dispose();
        SetBearerToken(request, refreshedAccessToken);
        return await base.SendAsync(request, cancellationToken);
    }

    // Renvoie le nouvel access token, ou null si la session est morte.
    
    private async Task<string?> TryRefreshAsync(string rejectedAccessToken, CancellationToken cancellationToken)
    {
        await RefreshLock.WaitAsync(cancellationToken);
        try
        {
            LoginResponse? currentSession = await _tokenStore.GetSessionAsync();
            if (currentSession is null)
            {
                return null; // déconnecté pendant qu'on attendait le verrou
            }

            if (currentSession.AccessToken != rejectedAccessToken)
            {
                // Une autre requête a déjà fait le refresh pendant qu'on attendait : on réutilise son token.
                return currentSession.AccessToken;
            }

            try
            {
                LoginResponse refreshedSession = await _authApiClient.RefreshAsync(currentSession.RefreshToken);
                await _tokenStore.SaveSessionAsync(refreshedSession);
                return refreshedSession.AccessToken;
            }
            catch (ApiException)
            {
                // Le serveur refuse le refresh token (expiré ou révoqué) : la session est morte.
                _tokenStore.ClearSession();
                return null;
            }
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    private static void SetBearerToken(HttpRequestMessage request, string accessToken) =>
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
}