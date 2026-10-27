using Microsoft.AspNetCore.Mvc; // side-note c'est cette biblio qui fournis tout les trucs d'api comme [route]
using TMS2000.Core.Services;
using TMS2000.Shared.DTO.Auth;
namespace TMS2000.Server.Controllers;

[ApiController]
// active plusieurs comportement de base de controller. exemple pour 400 => si le format n'est pas bon, répond automatiquement 400.

[Route("api/auth")]
// side note : [controller dans le nom de la route = passe le nom de la class du controller below mais sans "controller//

public class AuthController : ControllerBase
// héritage du controller de base de .net. ça donne accès à plein de fonction déjà présente et pas à re-écrire.
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService; // injection au niveau du constructeur de authService. qui proviens de program.cs. 
    }

    // below : DF la route login.
    [HttpPost("login")]
    // below : C'est des déclarations qui décrivent ce que le controlleur peut répondre, c'est ça qui va être visiblement sur swagger par exemple//.
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]

    public async Task<IActionResult> Login(LoginRequest request)
    {
        return Ok(await _authService.LoginAsync(request.Email, request.Password));
    }


    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]

    public async Task<IActionResult> Refresh(RefreshTokenRequest request) =>
        Ok(await _authService.RefreshAsync(request.RefreshToken));



    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout(RefreshTokenRequest request)
    {
        await _authService.LogoutAsync(request.RefreshToken);
        return NoContent();
    }
}

