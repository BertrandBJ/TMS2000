using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TMS2000.Core.Models;
using TMS2000.Core.Repositories;
using TMS2000.Shared.DTO.Auth;
using TMS2000.Core.Exceptions;
using TMS2000.Core.Helper;


namespace TMS2000.Core.Services;

public class AuthService
{
    private readonly UserRepository _userRepository;
    private readonly string _jwtSecret;
    private readonly int _jwtExpiryHours;
    private readonly int _jwtRefreshTokenDays;


    public AuthService(UserRepository userRepository, string jwtSecret, int jwtExpiryHours, int jwtRefreshTokenDays)
    {
        _userRepository = userRepository;
        _jwtSecret = jwtSecret; // correspond à la jwtSecret qui vient du .env et repris dans program.cs
        _jwtExpiryHours = jwtExpiryHours;
        _jwtRefreshTokenDays = jwtRefreshTokenDays;
    }

    // champs et constructeur : inchangés

    public async Task<LoginResponse> LoginAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(EmailNormalizer.Normalize(email));

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new InvalidCredentialsException();

        var now = DateTime.UtcNow;
        var (refreshToken, storedToken) = CreateRefreshToken(now);
        await _userRepository.AddRefreshTokenAsync(user.Id, storedToken);

        return BuildResponse(user, refreshToken, storedToken.ExpiresAt, now);
    }

    public async Task<LoginResponse> RefreshAsync(string refreshToken)
    {
        var now = DateTime.UtcNow;
        var (newRefreshToken, newStoredToken) = CreateRefreshToken(now);

        // Échange atomique : l'ancien token est consommé, le nouveau prend sa place
        // Pas oublier de hash le token qu'on send dans la methode, celui stored est déjà hashed, pas celle du client.
        var user = await _userRepository.RotateRefreshTokenAsync(
                       RefreshTokenGenerator.Hash(refreshToken), newStoredToken, now)
                   ?? throw new InvalidRefreshTokenException();

        return BuildResponse(user, newRefreshToken, newStoredToken.ExpiresAt, now);
    }

    public async Task LogoutAsync(string refreshToken) =>

        await _userRepository.RemoveRefreshTokenAsync(RefreshTokenGenerator.Hash(refreshToken));

    // Le token envoyé au client, et sa version stockée en base (le hash), also note : déclaration d'un tuple, return un tuple.
    private (string Token, RefreshToken Stored) CreateRefreshToken(DateTime now)
    {
        var token = RefreshTokenGenerator.Generate();
        var stored = new RefreshToken
        {
            TokenHash = RefreshTokenGenerator.Hash(token),
            CreatedAt = now,
            ExpiresAt = now.AddDays(_jwtRefreshTokenDays)
        };
        return (token, stored);
    }

    // La réponse commune au login et au refresh : nouveau JWT + refresh token
    private LoginResponse BuildResponse(User user, string refreshToken, DateTime refreshExpiresAt, DateTime now)
    {
        var accessExpiresAt = now.AddHours(_jwtExpiryHours);

        return new LoginResponse(
            AccessToken: GenerateJwt(user, accessExpiresAt),
            AccessTokenExpirationDate: accessExpiresAt,
            RefreshToken: refreshToken,
            RefreshTokenExpirationDate: refreshExpiresAt,
            Role: user.Role);
    }

    // GenerateJwt : inchangé

    private string GenerateJwt(User user, DateTime expiresAt)
    {
        var claims = new[] 
            
            // pour info => syntax new[] {X,Y,Z} => crée le tableau ET le remplis directement avec le contenue de {}.
            // c'est un "array initializer". bref c'est du sugar.
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };


        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // above : ici on définis comment on va signer la key. c'est juste one configuration, le calcule lui est fait à l'écriture du token below.

        var token = new JwtSecurityToken(
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);

        // c'est là que l'object token devient une chaine de character en format texte avec un header,payload,signature encodé en base64url (variable du base64 sans +/).
        // c'est ici que la signature est calculé.
    }
}












