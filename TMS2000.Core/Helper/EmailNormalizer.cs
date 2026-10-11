namespace TMS2000.Core.Helper;

public static class EmailNormalizer
{
    public static string Normalize(string email) => email.Trim().ToLowerInvariant(); // remplace l'écriture avec un methode
}