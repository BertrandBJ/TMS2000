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

    public async Task<LoginResponse> LoginAsync(string email, string password)
    {
        var normalizedEmail = EmailNormalizer.Normalize(email);
        var user = await _userRepository.GetByEmailAsync(normalizedEmail); // GetByEmailAsync provient de UsersRepository.cs.

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new InvalidCredentialsException();
        
        var now = DateTime.UtcNow;
        var accessTokenExpirationDate = now.AddHours(_jwtExpiryHours);
        var accessToken = GenerateJwt(user, accessTokenExpirationDate);
        var refreshTokenExpirationDate = now.AddDays(_jwtRefreshTokenDays);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        return new LoginResponse(

            AccessToken: accessToken,
            AccessTokenExpirationDate: accessTokenExpirationDate,
            RefreshToken : refreshToken,
            RefreshTokenExpirationDate : refreshTokenExpirationDate,
            Role : user.Role
        );
    }
    
    private string GenerateJwt(User user,DateTime expiresAt)
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












