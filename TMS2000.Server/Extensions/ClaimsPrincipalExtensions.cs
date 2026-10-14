using System.Security.Claims;

namespace TMS2000.Server.Extensions;

// on utilise des erreurs 500 parce que si y'a pas les claims dans un token c'est que le code de génération du token bug.

public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new InvalidOperationException("Claim NameIdentifier absent du token.");

    public static string GetEmail(this ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.Email)?.Value
        ?? throw new InvalidOperationException("Claim Email absent du token.");
}