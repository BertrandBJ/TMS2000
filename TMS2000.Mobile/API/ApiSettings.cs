namespace TMS2000.Mobile.API;

// Garde l'adresse de l'API sur le téléphone.
// Preferences = petit stockage clé/valeur d'Android, qui survit à la fermeture de l'app.
public class ApiSettings
{
    private const string BaseUrlPreferenceKey = "api_base_url";
    private const string DefaultBaseUrl = "http://10.6.0.14:5000/";

    public string BaseUrl
    {
        get => Preferences.Default.Get(BaseUrlPreferenceKey, DefaultBaseUrl);
        set => Preferences.Default.Set(BaseUrlPreferenceKey, NormalizeBaseUrl(value));
    }

    // Adresse complète d'une route, construite avec l'URL actuelle.
    // Appelée à chaque requête : un changement d'URL s'applique tout de suite, même aux clients déjà créés.
    // Ex. : BaseUrl "http://192.168.1.20:5000/" + "api/sync" → http://192.168.1.20:5000/api/sync
    
    public Uri BuildUri(string relativePath) => new Uri(new Uri(BaseUrl), relativePath);

    // "http://192.168.1.20:5000" ou "http://192.168.1.20:5000///" → "http://192.168.1.20:5000/"
    private static string NormalizeBaseUrl(string rawUrl) =>
        rawUrl.Trim().TrimEnd('/') + "/";
}