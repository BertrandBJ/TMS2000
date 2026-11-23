using TMS2000.Mobile.API;

namespace TMS2000.Mobile.Helpers;

public static class ApiErrorMessages
{
    public static bool IsKnown(Exception exception) =>
        exception is ApiException or HttpRequestException or TaskCanceledException;

    public static string Describe(Exception exception) => exception switch
    {
        ApiException apiException => apiException.Message,
        HttpRequestException => "Serveur injoignable.",
        TaskCanceledException => "Le serveur met trop de temps à répondre.",
        _ => "Erreur inattendue."
    };
}
