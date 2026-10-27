using System.Security.Cryptography;
using System.Text;

namespace TMS2000.Core.Helper;

public static class RefreshTokenGenerator
{
    // Token envoyé au client : 64 octets aléatoires crypto

    public static string Generate() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    // Ce qu'on stocke en base : le hash SHA-256, jamais le token en clair

    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}