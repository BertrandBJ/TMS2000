using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TMS2000.Core;
using TMS2000.Core.Services;
using TMS2000.Server.Extensions;
using TMS2000.Shared.DTO.Players;

namespace TMS2000.Server.Controllers;

[ApiController]
[Route("api/me")]
[Authorize(Roles = Roles.Player)]
public class MeController : ControllerBase
{
    private readonly PlayerService _playerService;

    public MeController(PlayerService playerService)
    {
        _playerService = playerService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMe() =>
        Ok(await _playerService.GetMeAsync(User.GetUserId()));
}