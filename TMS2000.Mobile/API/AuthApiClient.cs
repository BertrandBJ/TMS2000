using System.Net.Http.Json;
using TMS2000.Shared.DTO.Auth;
using TMS2000.Mobile.API;
namespace TMS2000.Mobile.API;

// pas de bearer ou de authhander ici, c'est pour le loging donc il faut que ce soit publique access.
public class AuthApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ApiSettings _apiSettings;

    // Le HttpClient est fourni par IHttpClientFactory, déjà configuré (timeout).
    
    public AuthApiClient(HttpClient httpClient, ApiSettings apiSettings)
    {
        _httpClient = httpClient;
        _apiSettings = apiSettings;
    }

    public async Task<LoginResponse> LoginAsync(string email, string password)
    {
        var loginRequest = new LoginRequest(email, password);
        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(_apiSettings.BuildUri("api/auth/login"), loginRequest);
        await ApiException.ThrowIfErrorAsync(response);
        return await ReadLoginResponseAsync(response);
    }

    public async Task<LoginResponse> RefreshAsync(string refreshToken)
    {
        var refreshRequest = new RefreshTokenRequest(refreshToken);
        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(_apiSettings.BuildUri("api/auth/refresh"), refreshRequest);
        await ApiException.ThrowIfErrorAsync(response);
        return await ReadLoginResponseAsync(response);
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var logoutRequest = new RefreshTokenRequest(refreshToken);
        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(_apiSettings.BuildUri("api/auth/logout"), logoutRequest);
        await ApiException.ThrowIfErrorAsync(response);
    }

    private static async Task<LoginResponse> ReadLoginResponseAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<LoginResponse>()
        ?? throw new InvalidOperationException("Réponse d'authentification vide.");
}