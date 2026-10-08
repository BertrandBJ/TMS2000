using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TMS2000.Core.Models;
using TMS2000.Core.Repositories;


namespace TMS2000.Core.Services;

public class AuthService
{
    private readonly UserRepository _userRepository;
    private readonly string _jwtSecret;
    private readonly int _jwtExpiryHours;

    public AuthService(UserRepository userRepository, string jwtSecret, int jwtExpiryHours)
    {
        _userRepository = userRepository;
        _jwtSecret = jwtSecret; // correspond à la jwtSecret qui vient du dockerfile et repris dans program.cs
        _jwtExpiryHours = jwtExpiryHours;
    }

    public async Task<string?> LoginAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email); // GetByEmailAsync provient de UsersRepository.cs.
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new UnauthorizedAccessException("Email ou mot de passe incorrect.");
        return GenerateJwt(user);
    }
    
    private string GenerateJwt(User user)
    {
        var claims = new[] // pour info => syntax new[] {X,Y,Z} => crée le tableau ET le remplis directement avec le contenue de {}. c'est un "array initializer". Bref c'est pour avoir un état zéro du tableau directement à la création.
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256); // alors ça c'est weird, ça demander de signer avec la key ok. Mais également à besoin d'un calcule mathématique de sécurité qui va correspondre à un résultat
        // mathématique de ce qu'il y'a à l'intérieur du token (genre les claims, expiration date and such.

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_jwtExpiryHours),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token); // c'est là que l'object token devient une chaine de character en format texte avec un header,payload,signature encodé en base64.
    }
}