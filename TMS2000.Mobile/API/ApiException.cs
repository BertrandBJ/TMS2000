using System.Net;
using System.Net.Http.Json;

namespace TMS2000.Mobile.API;

// Levée quand l'API a bien répondu, mais avec un code d'erreur (400, 401, 403, 404, 409, 422…).
// Les erreurs réseau (serveur injoignable, timeout) restent des HttpRequestException / TaskCanceledException.
public class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string? Title { get; }
    public string? Detail { get; }

    public ApiException(HttpStatusCode statusCode, string? title, string? detail)
        : base(detail ?? title ?? $"Erreur API ({(int)statusCode})")
    {
        StatusCode = statusCode;
        Title = title;
        Detail = detail;
    }

    // Ne fait rien si la réponse est un succès (2xx), sinon lit le ProblemDetails et lève une ApiException.
    
    public static async Task ThrowIfErrorAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        ProblemBody? problemBody = null;
        try
        {
            problemBody = await response.Content.ReadFromJsonAsync<ProblemBody>();
        }
        catch (Exception)
        {
            // Corps vide ou pas du JSON (ex. page d'erreur d'un proxy) : on garde juste le code HTTP.
        }

        throw new ApiException(response.StatusCode, problemBody?.Title, problemBody?.Detail);
    }

    // Les seuls champs du ProblemDetails qui nous intéressent.
    private sealed record ProblemBody(string? Title, string? Detail);
}