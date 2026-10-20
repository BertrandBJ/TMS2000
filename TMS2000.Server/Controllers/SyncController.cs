using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TMS2000.Core;
using TMS2000.Core.Services;
using TMS2000.Server.Extensions;
using TMS2000.Shared.DTO.Sync;

namespace TMS2000.Server.Controllers;

[ApiController]
[Route("api/sync")]
[Authorize(Roles = Roles.Player)] // seul un joueur a des actions offline à synchroniser
public class SyncController : ControllerBase
{
    private readonly SyncService _syncService;

    public SyncController(SyncService syncService)
    {
        _syncService = syncService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(SyncResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Sync(SyncRequest request) =>
        Ok(await _syncService.SyncAsync(User.GetUserId(), request));
}