using System.Net.Http.Json;
using System.Text.Json;
using TMS2000.Mobile.API;
using System.Text.Json.Serialization;

namespace TMS2000.Mobile.API;

// Client HTTP pour toutes les routes protégées par [Authorize] (sync, trades, market, admin…).
// Ses requêtes passent par l'AuthHandler, qui ajoute le token et gère le refresh :
// ce code n'a donc jamais à s'occuper des tokens.


public class ApiClient
{
    // Mêmes réglages JSON que le serveur : noms en camelCase + enums en texte ("Buy" et non 0).
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _httpClient;
    private readonly ApiSettings _apiSettings;

    public ApiClient(HttpClient httpClient, ApiSettings apiSettings)
    {
        _httpClient = httpClient;
        _apiSettings = apiSettings;
    }

    public async Task<TResponse> GetAsync<TResponse>(string relativePath)
    {   
        Uri requestUri = _apiSettings.BuildUri(relativePath);
        using HttpResponseMessage response = await _httpClient.GetAsync(requestUri);
        await ApiException.ThrowIfErrorAsync(response);
        return await ReadBodyAsync<TResponse>(response);
    }

    public async Task<TResponse> PostAsync<TRequest, TResponse>(string relativePath, TRequest requestBody)
    {
        Uri requestUri = _apiSettings.BuildUri(relativePath);
        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(requestUri, requestBody, JsonOptions);
        await ApiException.ThrowIfErrorAsync(response);
        return await ReadBodyAsync<TResponse>(response);
    }

    public async Task DeleteAsync(string relativePath)
    {
        Uri requestUri = _apiSettings.BuildUri(relativePath);
        using HttpResponseMessage response = await _httpClient.DeleteAsync(requestUri);
        await ApiException.ThrowIfErrorAsync(response);
    }

    private static async Task<TResponse> ReadBodyAsync<TResponse>(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions)
        ?? throw new InvalidOperationException($"Réponse vide pour {response.RequestMessage?.RequestUri}.");
}