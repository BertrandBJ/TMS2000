using Microsoft.AspNetCore.Mvc; // side-note c'est cette biblio qui fournis tout les trucs d'api comme [route]
using TMS2000.Core.Services;
using TMS2000.Shared.DTO.Auth;
namespace TMS2000.Server.Controllers;

[ApiController] // active plusieurs comportement de base comme des réponse 400 bad request and such.
[Route("api/[controller]")] // sert à Definir l'url. en gros controller est removed et c'est le reste du nom utilisé donc ici api/auth.

public class AuthentificationController : ControllerBase // héritage du controller de base de .net. ça donne accès à plein de fonction déjà présente et pas à re-écrire.
{
    private readonly AuthService _authService;

    public AuthentificationController(AuthService authService)
    {
        _authService = authService; // injection au niveau du constructeur de authService. qui proviens de program.cs. 
    }
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var token = await _authService.LoginAsync(request.Email, request.Password);
        if (token == null) return Unauthorized(new { message = "Email ou mot de passe incorrect." }); // Unauthorized send back 401 de base. avec le message à la suite.
        return Ok(new { token });  // à changer pour passer sur le global handler après.
    }
}